#!/usr/bin/env node
// Compute the semantic-release decision without invoking verify, prepare, or publish plugins.
import { execFile } from 'node:child_process';
import { mkdtemp, readFile, rm } from 'node:fs/promises';
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
  const configuration = JSON.parse(await readFile(join(process.cwd(), '.releaserc.json'), 'utf8'));
  const unsupportedAnalysis = ['extends', 'analyzeCommits', 'generateNotes']
    .filter((key) => Object.hasOwn(configuration, key));
  if (unsupportedAnalysis.length !== 0) {
    throw new Error(`Release planning does not support top-level ${unsupportedAnalysis.join(', ')}; `
      + 'inline shared configuration and put analysis options in the declared analysis plugin tuples.');
  }
  // Keep analysis options in sync with publication while excluding plugins with write hooks.
  const analysisPlugins = [
    '@semantic-release/commit-analyzer',
    '@semantic-release/release-notes-generator',
  ];
  const publicationPlugins = ['@semantic-release/exec', '@semantic-release/github'];
  for (const plugin of configuration.plugins) {
    const [name, options] = Array.isArray(plugin) ? plugin : [plugin, {}];
    if (!analysisPlugins.includes(name) && !publicationPlugins.includes(name)) {
      throw new Error(`Release planning does not support plugin ${name}; its analysis must be reviewed.`);
    }
    if (name === '@semantic-release/exec' && options
        && (Object.hasOwn(options, 'analyzeCommitsCmd') || Object.hasOwn(options, 'generateNotesCmd'))) {
      throw new Error('Release planning does not support exec analysis hooks; use the declared analysis plugins.');
    }
  }
  const plugins = configuration.plugins.filter((plugin) =>
    analysisPlugins.includes(Array.isArray(plugin) ? plugin[0] : plugin));
  for (const name of analysisPlugins) {
    if (!plugins.some((plugin) => (Array.isArray(plugin) ? plugin[0] : plugin) === name)) {
      throw new Error(`Release configuration must declare ${name} for planning.`);
    }
  }

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
  const { stdout: branch } = await run('git', ['branch', '--show-current'], {
    cwd: process.cwd(),
    env: plannerEnvironment,
  });
  const dispatchBranch = plannerEnvironment.GITHUB_REF?.startsWith('refs/heads/')
    ? plannerEnvironment.GITHUB_REF.slice('refs/heads/'.length)
    : 'main';
  const sourceBranch = branch.trim() || dispatchBranch;
  await run('git', ['clone', '--mirror', '--no-local', '--quiet', process.cwd(), mirrorPath], {
    env: plannerEnvironment,
  });
  await run('git', ['--git-dir', mirrorPath, 'update-ref', `refs/heads/${sourceBranch}`, head.trim()], {
    env: plannerEnvironment,
  });
  await run('git', ['--git-dir', mirrorPath, 'symbolic-ref', 'HEAD', `refs/heads/${sourceBranch}`], {
    env: plannerEnvironment,
  });

  const result = await semanticRelease(
    {
      branches: configuration.branches,
      tagFormat: configuration.tagFormat,
      repositoryUrl: pathToFileURL(mirrorPath).href,
      dryRun: true,
      ci: false,
      plugins,
      // semantic-release also reloads top-level hooks and shareable configs.
      extends: [],
      analyzeCommits: undefined,
      generateNotes: undefined,
      verifyConditions: [],
      verifyRelease: [],
      prepare: [],
      publish: [],
      addChannel: [],
      success: [],
      fail: [],
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
