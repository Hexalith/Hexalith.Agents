#!/usr/bin/env node
// Compute the semantic-release decision without invoking verify, prepare, or publish plugins.
import { execFile } from 'node:child_process';
import { mkdtemp, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { promisify } from 'node:util';
import { pathToFileURL } from 'node:url';
import semanticRelease from 'semantic-release';

const run = promisify(execFile);
const plannerEnvironment = { ...process.env };
delete plannerEnvironment.GH_TOKEN;
delete plannerEnvironment.GITHUB_TOKEN;

let temporaryDirectory;
try {
  temporaryDirectory = await mkdtemp(join(tmpdir(), 'agents-semantic-release-plan-'));
  const mirrorPath = join(temporaryDirectory, 'repository.git');
  const { stdout: isShallow } = await run('git', ['rev-parse', '--is-shallow-repository'], {
    cwd: process.cwd(),
    env: plannerEnvironment,
  });
  if (isShallow.trim() !== 'false') {
    throw new Error('Release planning requires complete non-shallow Git history and tags.');
  }
  const { stdout: head } = await run('git', ['rev-parse', '--verify', 'HEAD'], {
    cwd: process.cwd(),
    env: plannerEnvironment,
  });
  await run('git', ['clone', '--mirror', '--no-local', '--quiet', process.cwd(), mirrorPath], {
    env: plannerEnvironment,
  });
  await run('git', ['--git-dir', mirrorPath, 'update-ref', 'refs/heads/main', head.trim()], {
    env: plannerEnvironment,
  });
  await run('git', ['--git-dir', mirrorPath, 'symbolic-ref', 'HEAD', 'refs/heads/main'], {
    env: plannerEnvironment,
  });

  const result = await semanticRelease(
    {
      branches: ['main'],
      tagFormat: 'v${version}',
      repositoryUrl: pathToFileURL(mirrorPath).href,
      dryRun: true,
      ci: false,
      plugins: [
        '@semantic-release/commit-analyzer',
        '@semantic-release/release-notes-generator',
      ],
    },
    {
      cwd: process.cwd(),
      env: plannerEnvironment,
      stdout: process.stderr,
      stderr: process.stderr,
    },
  );

  const next = result === false ? null : result.nextRelease;
  process.stdout.write(`${JSON.stringify({ release_required: next !== null, version: next?.version ?? null })}\n`);
} finally {
  if (temporaryDirectory !== undefined) {
    await rm(temporaryDirectory, { recursive: true, force: true });
  }
}
