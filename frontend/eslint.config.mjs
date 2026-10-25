import nx from '@nx/eslint-plugin'
import enforceEsPrivate from 'eslint-plugin-enforce-es-private'
import stylistic from '@stylistic/eslint-plugin'
import angular from 'angular-eslint'

const typeDependencies = {
	'type:app': ['type:feature', 'type:ui', 'type:data-access', 'type:util'],
	'type:feature': ['type:ui', 'type:data-access', 'type:util'],
	'type:ui': ['type:ui', 'type:util'],
	'type:data-access': ['type:data-access', 'type:util'],
	'type:util': ['type:util'],
}

const scopeDependencies = {
	'scope:app': ['scope:games', 'scope:maps', 'scope:media', 'scope:shared'],
	'scope:games': ['scope:games', 'scope:media', 'scope:shared'],
	'scope:maps': ['scope:maps', 'scope:media', 'scope:shared'],
	'scope:media': ['scope:media', 'scope:shared'],
	'scope:shared': ['scope:shared'],
}

const platformDependencies = {
	'platform:admin': ['platform:admin', 'platform:shared'],
	'platform:web': ['platform:web', 'platform:shared'],
	'platform:shared': ['platform:shared'],
}

// A source must match all three dimensions to receive cross-project permissions.
const depConstraints = Object.entries(typeDependencies).flatMap(([type, allowedTypes]) =>
	Object.entries(scopeDependencies).flatMap(([scope, allowedScopes]) =>
		Object.entries(platformDependencies).flatMap(([platform, allowedPlatforms]) => [
			{ allSourceTags: [type, scope, platform], onlyDependOnLibsWithTags: allowedTypes },
			{ allSourceTags: [type, scope, platform], onlyDependOnLibsWithTags: allowedScopes },
			{ allSourceTags: [type, scope, platform], onlyDependOnLibsWithTags: allowedPlatforms },
		]),
	),
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
		plugins: {
			'@angular-eslint': angular.tsPlugin,
		},
		rules: {
			'@angular-eslint/prefer-signals': 'error',
			'@angular-eslint/prefer-output-readonly': 'error',
			'@angular-eslint/prefer-output-emitter-ref': 'error',
			'@angular-eslint/prefer-inject': 'error',
			'no-restricted-imports': [
				'error',
				{
					paths: [
						{
							name: '@angular/forms',
							importNames: [
								'FormsModule',
								'ReactiveFormsModule',
								'FormControl',
								'FormGroup',
								'FormArray',
								'FormRecord',
								'FormBuilder',
								'NonNullableFormBuilder',
								'UntypedFormControl',
								'UntypedFormGroup',
								'UntypedFormArray',
								'UntypedFormBuilder',
								'NgModel',
								'NgForm',
								'NgModelGroup',
								'FormControlDirective',
								'FormControlName',
								'FormGroupDirective',
								'FormGroupName',
								'FormArrayName',
							],
							message: 'Use Signal Forms from @angular/forms/signals.',
						},
					],
				},
			],
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
	{
		files: ['**/*.ts', '**/*.tsx', '**/*.cts', '**/*.mts', '**/*.js', '**/*.jsx', '**/*.cjs', '**/*.mjs'],
		plugins: {
			'@stylistic': stylistic,
		},

		rules: {
			'@stylistic/padding-line-between-statements': [
				'error',
				{
					blankLine: 'always',
					prev: ['const', 'let'],
					next: '*',
				},
				{
					blankLine: 'any',
					prev: ['const', 'let'],
					next: ['const', 'let'],
				},
				{
					blankLine: 'always',
					prev: '*',
					next: 'return',
				},
				{
					blankLine: 'always',
					prev: ['if', 'for', 'while', 'switch', 'try'],
					next: '*',
				},
			],
		},
	},
]
