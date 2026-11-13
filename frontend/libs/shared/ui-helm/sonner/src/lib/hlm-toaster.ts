import type { BooleanInput, NumberInput } from '@angular/cdk/coercion'
import { Component, booleanAttribute, computed, input, numberAttribute } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matCheckCircleRound,
	matInfoRound,
	matProgressActivityRound,
	matErrorRound,
	matWarningRound,
} from '@ng-icons/material-symbols/round'
import { BrnSonnerImports, type ToasterProps } from '@spartan-ng/brain/sonner'
import { HlmButtonImports } from '@spartan-ng/helm/button'
import { hlm } from '@spartan-ng/helm/utils'
import type { ClassValue } from 'clsx'

@Component({
	selector: 'hlm-toaster',
	exportAs: 'hlmToaster',
	imports: [BrnSonnerImports, HlmButtonImports, NgIcon],
	templateUrl: './hlm-toaster.html',
	providers: [
		provideIcons({ matCheckCircleRound, matInfoRound, matWarningRound, matErrorRound, matProgressActivityRound }),
	],
})
export class HlmToaster {
	public readonly invert = input<ToasterProps['invert'], BooleanInput>(false, {
		transform: booleanAttribute,
	})

	public readonly theme = input<ToasterProps['theme']>('light')
	public readonly position = input<ToasterProps['position']>('bottom-right')
	public readonly hotKey = input<ToasterProps['hotkey']>(['altKey', 'KeyT'])

	public readonly richColors = input<ToasterProps['richColors'], BooleanInput>(false, {
		transform: booleanAttribute,
	})

	public readonly expand = input<ToasterProps['expand'], BooleanInput>(false, {
		transform: booleanAttribute,
	})

	public readonly duration = input<ToasterProps['duration'], NumberInput>(4000, {
		transform: numberAttribute,
	})

	public readonly visibleToasts = input<ToasterProps['visibleToasts'], NumberInput>(3, {
		transform: numberAttribute,
	})

	public readonly closeButton = input<ToasterProps['closeButton'], BooleanInput>(false, {
		transform: booleanAttribute,
	})

	public readonly toastOptions = input<ToasterProps['toastOptions']>({})

	protected readonly computedToastOptions = computed(() => {
		const options = this.toastOptions()

		return {
			...options,
			classes: {
				...options?.classes,
				toast: hlm('rounded-2xl!', options?.classes?.toast),
			},
		}
	})

	public readonly offset = input<ToasterProps['offset']>(null)
	public readonly userClass = input<ClassValue>('', { alias: 'class' })

	public readonly userStyle = input<Record<string, string>>(
		{
			'--normal-bg': 'var(--popover)',
			'--normal-text': 'var(--popover-foreground)',
			'--normal-border': 'var(--border)',
			'--border-radius': 'var(--radius)',
		},
		{ alias: 'style' },
	)

	protected readonly computedClass = computed(() => hlm('toaster group', this.userClass()))
}
