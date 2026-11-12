import { computed, Directive, input } from '@angular/core'
import { BrnToggleGroupItem } from '@spartan-ng/brain/toggle-group'
import { toggleVariants, ToggleVariants } from '@spartan-ng/helm/toggle'
import { classes } from '@spartan-ng/helm/utils'
import { injectHlmToggleGroup } from './hlm-toggle-group.token'

@Directive({
	selector: 'button[hlmToggleGroupItem]',
	exportAs: 'hlmToggleGroupItem',
	hostDirectives: [
		{
			directive: BrnToggleGroupItem,
			inputs: ['id', 'value', 'disabled', 'state', 'aria-label', 'type'],
			outputs: ['stateChange'],
		},
	],
	host: {
		'data-slot': 'toggle-group-item',
		'[attr.data-variant]': 'resolvedVariant()',
		'[attr.data-size]': 'resolvedSize()',
		'[attr.data-spacing]': 'toggleGroup.spacing()',
	},
})
export class HlmToggleGroupItem {
	readonly variant = input<ToggleVariants['variant']>('default')
	readonly size = input<ToggleVariants['size']>('default')

	protected readonly toggleGroup = injectHlmToggleGroup()

	protected readonly resolvedVariant = computed(() => this.toggleGroup.variant() || this.variant())
	protected readonly resolvedSize = computed(() => this.toggleGroup.size() || this.size())

	constructor() {
		classes(() => [
			'data-[state=on]:bg-muted group-data-[spacing=0]/toggle-group:px-3 group-data-horizontal/toggle-group:data-[spacing=0]:first:rounded-s-3xl group-data-vertical/toggle-group:data-[spacing=0]:first:rounded-t-3xl group-data-horizontal/toggle-group:data-[spacing=0]:last:rounded-e-3xl group-data-vertical/toggle-group:data-[spacing=0]:last:rounded-b-3xl shrink-0 group-data-[spacing=0]/toggle-group:rounded-none group-data-[spacing=0]/toggle-group:shadow-none focus:z-10 focus-visible:z-10 group-data-horizontal/toggle-group:data-[spacing=0]:data-[variant=outline]:border-s-0 group-data-vertical/toggle-group:data-[spacing=0]:data-[variant=outline]:border-t-0 group-data-horizontal/toggle-group:data-[spacing=0]:data-[variant=outline]:first:border-s group-data-vertical/toggle-group:data-[spacing=0]:data-[variant=outline]:first:border-t',
			toggleVariants({
				variant: this.resolvedVariant(),
				size: this.resolvedSize(),
			}),
		])
	}
}
