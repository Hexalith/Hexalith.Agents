/** Retain the original immutable release set before NuGet publication. */
import { createHash } from 'node:crypto';
import { readFile, writeFile } from 'node:fs/promises';
import { execFileSync } from 'node:child_process';

function run(command, args) {
  return execFileSync(command, args, { encoding: 'utf8', maxBuffer: 4 * 1024 * 1024 });
}

function identity(context) {
  const version = context.nextRelease.version;
  const sha = context.nextRelease.gitHead;
  const repository = process.env.GITHUB_REPOSITORY;
  if (!/^[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z.-]+)?$/.test(version)
      || !/^[0-9a-f]{40}$/.test(sha) || sha !== process.env.GITHUB_SHA
      || !/^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(repository ?? '')) {
    throw new Error('Release staging requires the exact original repository, version and source SHA.');
  }
  return { version, sourceSha: sha, repository, tag: `v${version}` };
}

export async function prepare(_config, context) {
  const source = identity(context);
  const paths = run('python3', ['scripts/validate-nuget-packages.py', './nupkgs', '--publication-version', source.version])
    .trim().split('\n');
  const archives = [];
  for (const path of paths) {
    const bytes = await readFile(path);
    archives.push({ name: path.split('/').at(-1), size: bytes.length,
      sha256: createHash('sha256').update(bytes).digest('hex') });
  }
  const provenance = { schemaVersion: 1, ...source, archives };
  const provenancePath = 'nupkgs/release-provenance.json';
  await writeFile(provenancePath, `${JSON.stringify(provenance, null, 2)}\n`);
  const notes = 'nupkgs/release-notes.md';
  await writeFile(notes, context.nextRelease.notes ?? '');
  // Create must fail on a pre-existing release. Normal release runs never resume a collision.
  run('gh', ['release', 'create', source.tag, ...paths, provenancePath,
    '--repo', source.repository, '--draft', '--target', source.sourceSha,
    '--title', source.tag, '--notes-file', notes]);
}

export async function publish(_config, context) {
  const source = identity(context);
  // Re-read the retained draft and prove its original archives, exact tag/source and complete remote set.
  // verify-only cannot push. Finalization occurs only after every remote package content matches.
  run('python3', ['scripts/recover-release-packages.py', source.version,
    '--source-sha', source.sourceSha, '--verify-only']);
  run('gh', ['release', 'edit', source.tag, '--repo', source.repository, '--draft=false']);
  return { name: 'GitHub Release', url: `https://github.com/${source.repository}/releases/tag/${source.tag}` };
}
