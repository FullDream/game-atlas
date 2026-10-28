import {
	booleanAttribute,
	Component,
	computed,
	DestroyRef,
	ElementRef,
	HOST_TAG_NAME,
	inject,
	input,
} from '@angular/core'
import { takeUntilDestroyed } from '@angular/core/rxjs-interop'
import { NgIcon, type IconType } from '@ng-icons/core'
import { HlmButton } from '@spartan-ng/helm/button'
import { HlmSpinner } from '@spartan-ng/helm/spinner'
import { filter, fromEvent } from 'rxjs'

@Component({
	selector: 'a[ui-button], button[ui-button]',
	imports: [NgIcon, HlmSpinner],
	templateUrl: './button.html',
	hostDirectives: [{ directive: HlmButton, inputs: ['variant', 'size'] }],
	host: {
		'[attr.aria-busy]': 'loading() || null',
		'[attr.aria-disabled]': 'isDisabled() || null',
		'[attr.data-disabled]': 'isDisabled() || null',
		'[attr.disabled]': '!isAnchor && isDisabled() ? "" : null',
		'[attr.tabindex]': 'isDisabled() ? -1 : null',
	},
})
export class Button {
	readonly disabled = input(false, { transform: booleanAttribute })
	readonly loading = input(false, { transform: booleanAttribute })
	readonly icon = input<IconType>()

	readonly #button = inject(HlmButton)
	readonly #element = inject(ElementRef<HTMLElement>)
	readonly #destroy = inject(DestroyRef)
	protected readonly isAnchor = inject(HOST_TAG_NAME) === 'a'

	protected readonly isDisabled = computed(() => this.loading() || this.disabled())

	constructor() {
		this.#button.setClass('relative inline-grid place-items-center')

		fromEvent<MouseEvent>(this.#element.nativeElement, 'click', { capture: true })
			.pipe(
				filter(() => this.isDisabled()),
				takeUntilDestroyed(this.#destroy),
			)
			.subscribe(event => {
				event.preventDefault()
				event.stopImmediatePropagation()
			})
	}
}
