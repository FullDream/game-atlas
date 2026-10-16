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
							sourceTag: '*',
							onlyDependOnLibsWithTags: ['*'],
						},
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
