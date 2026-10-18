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
