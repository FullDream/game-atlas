import nx from '@nx/eslint-plugin'
import enforceEsPrivate from 'eslint-plugin-enforce-es-private'

export default [
	...nx.configs['flat/base'],
	...nx.configs['flat/typescript'],
	...nx.configs['flat/javascript'],
	enforceEsPrivate.configs.recommended,
	{
		ignores: ['**/dist', '**/out-tsc'],
	},
	{
		files: ['**/*.ts', '**/*.tsx', '**/*.js', '**/*.jsx'],
		rules: {
			'@nx/enforce-module-boundaries': [
				'error',
				{
					enforceBuildableLibDependency: true,
					allow: ['^.*/eslint(\\.base)?\\.config\\.[cm]?[jt]s$'],
					depConstraints: [
						{
							sourceTag: 'type:app',
							onlyDependOnLibsWithTags: ['type:feature', 'type:ui', 'type:data-access', 'type:util'],
						},
						{
							sourceTag: 'type:feature',
							onlyDependOnLibsWithTags: ['type:ui', 'type:data-access', 'type:util'],
						},
						{ sourceTag: 'type:ui', onlyDependOnLibsWithTags: ['type:ui', 'type:util'] },
						{
							sourceTag: 'type:data-access',
							onlyDependOnLibsWithTags: ['type:data-access', 'type:util'],
						},
						{ sourceTag: 'type:util', onlyDependOnLibsWithTags: ['type:util'] },

						// Application scopes: web and editor share only scope:shared libraries.
						{ sourceTag: 'scope:shared', onlyDependOnLibsWithTags: ['scope:shared'] },
						{ sourceTag: 'scope:web', onlyDependOnLibsWithTags: ['scope:web', 'scope:shared'] },
						{ sourceTag: 'scope:editor', onlyDependOnLibsWithTags: ['scope:editor', 'scope:shared'] },
					],
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
			'@typescript-eslint/interface-name-prefix': 'off',
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
	{
		files: ['**/*.ts', '**/*.tsx', '**/*.cts', '**/*.mts', '**/*.js', '**/*.jsx', '**/*.cjs', '**/*.mjs'],
		// Override or add rules here
		rules: {},
	},
]
