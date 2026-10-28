import assert from 'node:assert/strict'
import { spawnSync } from 'node:child_process'
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { dirname, join } from 'node:path'
import { test } from 'node:test'
import { checkDocumentation, markdown } from './check-docs.mjs'

function fixture(t, files) {
	const root = mkdtempSync(join(tmpdir(), 'game-atlas-docs-'))
	t.after(() => rmSync(root, { recursive: true, force: true }))

	for (const [file, text] of Object.entries(files)) {
		mkdirSync(dirname(join(root, file)), { recursive: true })
		writeFileSync(join(root, file), text)
	}

	return { root, check: () => checkDocumentation(root, Object.keys(files)) }
}

test('checks relative paths, encoded spaces, images, repository paths and same-page anchors', t => {
	const { check } = fixture(t, {
		'AGENTS.md':
			'# Guidance\n[topic](docs/topic.md#c) [root](/docs/topic.md) [self](#guidance)\n![image](docs/diagram.svg)',
		'docs/topic.md': '# C#\n[space](<a file.md#два-слова>) [encoded](a%20file.md#два-слова)',
		'docs/a file.md': '# Два слова',
		'docs/diagram.svg': '<svg/>',
	})
	const result = check()

	assert.deepEqual(result.errors, [])
	assert.equal(result.links, 6)
})

test('reports missing files and anchors with source line numbers', t => {
	const { check } = fixture(t, {
		'AGENTS.md': '# Guidance\n[missing](absent.md)\n[anchor](docs/topic.md#absent)',
		'docs/topic.md': '# Existing',
	})
	const { errors } = check()

	assert.equal(errors.length, 2)
	assert.match(errors[0], /^AGENTS\.md:2: missing target:/)
	assert.match(errors[1], /^AGENTS\.md:3: missing Markdown anchor:/)
})

test('ignores example links in code/comments and makes no requests for external URLs', t => {
	const { check } = fixture(t, {
		'AGENTS.md':
			'# Guidance\n`[inline](missing.md)`\n```md\n[example](missing.md)\n```\n~~~\n[example](missing.md)\n~~~\n<!-- [comment](missing.md) -->\n[web](https://example.invalid) [mail](mailto:user@example.invalid) [web](//example.invalid)',
	})
	const result = check()

	assert.deepEqual(result.errors, [])
	assert.equal(result.links, 0)
})

test('supports duplicate heading suffixes, Setext headings and explicit HTML anchors', () => {
	const { anchors } = markdown('# Title\n# Title\n# Title-1\n# Title\nSetext\n======\n<a id="custom"></a>')

	assert.deepEqual([...anchors], ['title', 'title-1', 'title-1-1', 'title-2', 'setext', 'custom'])
})

test('supports reference links while treating unresolved references as ordinary Markdown text', t => {
	const { check } = fixture(t, {
		'AGENTS.md':
			'# Heading\n[label][target] [target][] [target]\n[missing][undefined]\n\n[target]: docs/topic.md "Topic"',
		'docs/topic.md': '# Topic',
	})
	const result = check()

	assert.equal(result.links, 4)
	assert.deepEqual(result.errors, [])
})

test('parses emphasis and inline code in headings, indented code and multiline reference definitions', t => {
	const { check } = fixture(t, {
		'AGENTS.md':
			'[heading](docs/topic.md#emphasis-_code_)\n[reference][multiline]\n\n[multiline]:\n  docs/topic.md\n\n    [example](missing.md)',
		'docs/topic.md': '# _Emphasis_ `_code_`',
	})
	const result = check()

	assert.deepEqual(result.errors, [])
	assert.equal(result.links, 3)
})

test('handles nested brackets, balanced parentheses and escaped filenames', t => {
	const { check } = fixture(t, {
		'AGENTS.md': '[a [nested] label](docs/a(b).md)\n[escaped](docs/a\\(b\\).md)',
		'docs/a(b).md': '# Topic',
	})

	assert.deepEqual(check().errors, [])
})

test('rejects malformed URL escapes and targets outside the repository', t => {
	const { check } = fixture(t, { 'AGENTS.md': '[broken](bad%GG.md)\n[outside](../outside.md)' })
	const { errors } = check()

	assert.equal(errors.length, 2)
	assert.match(errors[0], /invalid URL encoding/)
	assert.match(errors[1], /outside the repository/)
})

test('skips imported skills and archived changes, but checks links pointing into skills', t => {
	const { check } = fixture(t, {
		'AGENTS.md': '[skill](.agents/skills/example/SKILL.md#example)',
		'.agents/skills/example/SKILL.md': '# Example\n[template](not-installed.md)',
		'openspec/changes/archive/old/design.md': '[historical](deleted.md)',
	})
	const result = check()

	assert.deepEqual(result.errors, [])
	assert.equal(result.documents, 1)
	assert.equal(result.links, 1)
})

test('checks Codex helper paths relative to config.toml', t => {
	const { check } = fixture(t, {
		'frontend/.codex/config.toml':
			'[agents.first]\nconfig_file = "agents/first.toml"\n[agents.missing]\nconfig_file = \'agents/missing.toml\'',
		'frontend/.codex/agents/first.toml': 'developer_instructions = "Helper"',
	})
	const result = check()

	assert.equal(result.configReferences, 2)
	assert.equal(result.errors.length, 1)
	assert.match(result.errors[0], /frontend\/.codex\/config.toml:4: missing target: agents\/missing.toml/)
})

test('runner includes untracked documents, ignores deleted tracked files and returns a failing exit code', t => {
	const { root } = fixture(t, { 'AGENTS.md': '[missing](missing.md)' })
	const command = `import { runDocumentationCheck } from ${JSON.stringify(new URL('./check-docs.mjs', import.meta.url).href)}; runDocumentationCheck(${JSON.stringify(root)})`
	assert.equal(spawnSync('git', ['init', '--quiet'], { cwd: root }).status, 0)
	writeFileSync(join(root, 'deleted.md'), '# Gone')
	assert.equal(spawnSync('git', ['add', 'deleted.md'], { cwd: root }).status, 0)
	rmSync(join(root, 'deleted.md'))
	const run = spawnSync(process.execPath, ['--input-type=module', '-e', command], { cwd: tmpdir(), encoding: 'utf8' })

	assert.equal(run.status, 1)
	assert.match(run.stderr, /AGENTS\.md:1: missing target: missing.md/)
	assert.doesNotMatch(run.stderr, /deleted.md/)
	writeFileSync(join(root, 'missing.md'), '# Exists')
	const success = spawnSync(process.execPath, ['--input-type=module', '-e', command], {
		cwd: tmpdir(),
		encoding: 'utf8',
	})

	assert.equal(success.status, 0, success.stderr)
	assert.match(success.stdout, /Documentation check passed/)
})
