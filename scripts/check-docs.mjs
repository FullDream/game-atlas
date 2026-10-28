import { execFileSync } from 'node:child_process'
import { existsSync, readFileSync, realpathSync, statSync } from 'node:fs'
import { dirname, isAbsolute, relative, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import GithubSlugger from 'github-slugger'
import { fromMarkdown } from 'mdast-util-from-markdown'
import { toString } from 'mdast-util-to-string'

const repositoryRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const excludedDirectories = new Set([
	'.agents',
	'.codex',
	'.git',
	'.idea',
	'node_modules',
	'bin',
	'obj',
	'dist',
	'out-tsc',
	'coverage',
	'generated',
])

function visit(node, action) {
	action(node)

	for (const child of node.children ?? []) visit(child, action)
}

export function markdown(source) {
	const tree = fromMarkdown(source)
	const slugger = new GithubSlugger()
	const references = new Map()
	const links = []
	const anchors = new Set()

	visit(tree, node => {
		if (node.type === 'definition' && !references.has(node.identifier)) references.set(node.identifier, node.url)
	})

	visit(tree, node => {
		if (node.type === 'heading') anchors.add(slugger.slug(toString(node, { includeHtml: false })))

		if (node.type === 'link' || node.type === 'image' || node.type === 'definition') {
			links.push({ url: node.url, line: node.position.start.line })
		} else if (node.type === 'linkReference' || node.type === 'imageReference') {
			links.push({ url: references.get(node.identifier), line: node.position.start.line })
		} else if (node.type === 'html') {
			const html = node.value.replace(/<!--[^]*?-->/g, '')

			for (const tag of html.matchAll(/<(?:a|h[1-6])\b[^>]*>/gi)) {
				for (const attribute of tag[0].matchAll(/\b(?:id|name)\s*=\s*["']([^"']+)["']/gi))
					anchors.add(attribute[1])
			}
		}
	})

	return { anchors, links }
}

function isInside(root, target) {
	const path = relative(root, target)

	return path !== '..' && !path.startsWith('../') && !path.startsWith('..\\') && !isAbsolute(path)
}

export function checkDocumentation(root, files) {
	const errors = []
	const cache = new Map()
	let documents = 0
	let links = 0
	let configReferences = 0
	const parse = file => {
		if (!cache.has(file)) cache.set(file, markdown(readFileSync(file, 'utf8')))

		return cache.get(file)
	}
	const check = (file, line, url, literal = false) => {
		const base = dirname(resolve(root, file))
		let target
		let fragment

		try {
			if (literal) target = resolve(base, url)
			else {
				const hash = url.indexOf('#')
				const pathname = (hash < 0 ? url : url.slice(0, hash)).split('?')[0]
				fragment = hash < 0 ? '' : decodeURIComponent(url.slice(hash + 1))
				const decoded = decodeURIComponent(pathname)
				target = pathname
					? resolve(decoded.startsWith('/') ? root : base, decoded.replace(/^\//, ''))
					: resolve(root, file)
			}
		} catch {
			errors.push(`${file}:${line}: invalid URL encoding in ${url}`)

			return
		}

		if (!isInside(root, target) || (existsSync(target) && !isInside(realpathSync(root), realpathSync(target))))
			errors.push(`${file}:${line}: target is outside the repository: ${url}`)
		else if (!existsSync(target)) errors.push(`${file}:${line}: missing target: ${url}`)
		else if (fragment && /\.md$/i.test(target) && statSync(target).isFile() && !parse(target).anchors.has(fragment))
			errors.push(`${file}:${line}: missing Markdown anchor: ${url}`)
	}

	for (const file of [...new Set(files)].sort()) {
		const absolute = resolve(root, file)
		if (!existsSync(absolute) || !statSync(absolute).isFile()) continue
		const portable = file.replace(/\\/g, '/')
		if (
			/\.md$/i.test(file) &&
			!portable.split('/').some(part => excludedDirectories.has(part)) &&
			!portable.startsWith('openspec/changes/archive/')
		) {
			documents++

			for (const link of parse(absolute).links) {
				if (!/^(?:[a-z][a-z\d+.-]*:|\/\/)/i.test(link.url)) {
					links++
					check(file, link.line, link.url)
				}
			}
		}

		if (/(^|\/)\.codex\/config\.toml$/.test(portable)) {
			const source = readFileSync(absolute, 'utf8')
			for (const match of source.matchAll(/^\s*config_file\s*=\s*("(?:\\.|[^"\\])*"|'[^']*')/gm)) {
				const url = match[1].startsWith('"') ? JSON.parse(match[1]) : match[1].slice(1, -1)
				const line = source.slice(0, match.index).split('\n').length
				configReferences++
				check(file, line, url, true)
			}
		}
	}

	return { documents, links, configReferences, errors }
}

export function runDocumentationCheck(root) {
	const files = execFileSync('git', ['ls-files', '--cached', '--others', '--exclude-standard', '-z'], {
		cwd: root,
		encoding: 'utf8',
	})
		.split('\0')
		.filter(Boolean)
	const result = checkDocumentation(root, files)

	if (result.errors.length) {
		console.error(result.errors.join('\n'))
		console.error(
			`Documentation check failed: ${result.errors.length} error(s). Fix the target path, heading or reference in the reported source file.`,
		)
		process.exitCode = 1
	} else
		console.log(
			`Documentation check passed: ${result.documents} documents, ${result.links} local links, ${result.configReferences} agent configuration references.`,
		)
}

function main() {
	if (process.argv.includes('--help')) {
		console.log(
			'Usage: node scripts/check-docs.mjs\nChecks local Markdown links, images, heading anchors, reference links and Codex config_file targets.\nSkips imported skills, build output and archived OpenSpec changes. Install tooling with npm ci --prefix scripts; checks make no network requests.',
		)

		return
	}

	if (process.argv.length > 2) throw new Error('Unknown arguments. Use --help for usage.')
	runDocumentationCheck(repositoryRoot)
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
	try {
		main()
	} catch (error) {
		console.error(`Documentation check failed: ${error.message}`)
		process.exitCode = 1
	}
}
