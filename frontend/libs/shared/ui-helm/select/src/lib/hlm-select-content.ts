import { BooleanInput } from '@angular/cdk/coercion'
import { booleanAttribute, Component, computed, input } from '@angular/core'
import { BrnSelectContent, BrnSelectList } from '@spartan-ng/brain/select'
import { classes, hlm } from '@spartan-ng/helm/utils'
import { HlmSelectScrollDown } from './hlm-select-scroll-down'
import { HlmSelectScrollUp } from './hlm-select-scroll-up'

@Component({
	selector: 'hlm-select-content',
	exportAs: 'hlmSelectContent',
	imports: [HlmSelectScrollUp, HlmSelectScrollDown, BrnSelectList],
	templateUrl: './hlm-select-content.html',
	hostDirectives: [BrnSelectContent],
})
export class HlmSelectContent {
	protected readonly computedListboxClasses = computed(() => hlm('flex flex-col'))

	readonly showScroll = input<boolean, BooleanInput>(false, { transform: booleanAttribute })

	constructor() {
		classes(
			() =>
				'bg-popover no-scrollbar text-popover-foreground data-open:animate-in data-closed:animate-out data-closed:fade-out-0 data-open:fade-in-0 data-closed:zoom-out-95 data-open:zoom-in-95 data-[side=bottom]:slide-in-from-top-2 data-[side=left]:slide-in-from-right-2 data-[side=right]:slide-in-from-left-2 data-[side=top]:slide-in-from-bottom-2 ring-foreground/5 max-h-72 min-w-36 rounded-2xl shadow-2xl relative flex w-(--brn-select-width) flex-col overflow-x-hidden overflow-y-auto ring-1 duration-100',
		)
	}
}
