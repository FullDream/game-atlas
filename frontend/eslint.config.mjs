import nx from '@nx/eslint-plugin'
import enforceEsPrivate from 'eslint-plugin-enforce-es-private'

const typeDependencies = {
	'type:app': ['type:feature', 'type:ui', 'type:data-access', 'type:util'],
	'type:feature': ['type:ui', 'type:data-access', 'type:util'],
	'type:ui': ['type:ui', 'type:util'],
	'type:data-access': ['type:data-access', 'type:util'],
	'type:util': ['type:util'],
}

const scopeDependencies = {
	'scope:shared': ['scope:shared'],
	'scope:web': ['scope:web', 'scope:shared'],
	'scope:editor': ['scope:editor', 'scope:shared'],
}

// A source must match both tag dimensions. Nx rejects cross-project imports
// when no constraint matches, so a missing type or scope cannot bypass a layer.
const depConstraints = Object.entries(typeDependencies).flatMap(([type, allowedTypes]) =>
	Object.entries(scopeDependencies).flatMap(([scope, allowedScopes]) => [
		{ allSourceTags: [type, scope], onlyDependOnLibsWithTags: allowedTypes },
		{ allSourceTags: [type, scope], onlyDependOnLibsWithTags: allowedScopes },
	]),
)

export default [
	...nx.configs['flat/base'],
	...nx.configs['flat/typescript'],
	...nx.configs['flat/javascript'],
	enforceEsPrivate.configs.recommended,
	{
		ignores: ['**/dist', '**/out-tsc'],
	},
	{
		files: ['**/*.ts', '**/*.tsx', '**/*.cts', '**/*.mts', '**/*.js', '**/*.jsx', '**/*.cjs', '**/*.mjs'],
		rules: {
			'@nx/enforce-module-boundaries': [
				'error',
				{
					allow: ['^.*/eslint(\\.base)?\\.config\\.[cm]?[jt]s$'],
					depConstraints,
				},
			],
		},
	},
	{
		files: ['**/*.ts', '**/*.tsx', '**/*.cts', '**/*.mts'],
		rules: {
			'@typescript-eslint/naming-convention': [
				'error',
				{
					selector: 'default',
					format: null,
					leadingUnderscore: 'forbid',
				},
			],
			'@typescript-eslint/explicit-module-boundary-types': 'off',
			'@typescript-eslint/no-explicit-any': 'error',
			'@typescript-eslint/no-empty-function': 'off',
			'@typescript-eslint/explicit-function-return-type': [
				'error',
				{
					allowExpressions: true,
				},
			],
		},
	},
]
