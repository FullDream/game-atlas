import { NgOptimizedImage } from '@angular/common'
import { Component, computed, input, signal } from '@angular/core'

import { NgIcon, provideIcons } from '@ng-icons/core'
import { matBrokenImageRound } from '@ng-icons/material-symbols/round'

export type PickerImage = { id: string; name: string; url: string; alt: string; placeholder: string | null }

@Component({
	selector: 'ui-media-thumbnail',
	imports: [NgOptimizedImage, NgIcon],
	templateUrl: './media-thumbnail.html',
	providers: [provideIcons({ matBrokenImageRound })],
	host: {
		class: 'relative block overflow-hidden bg-muted',
		'[attr.aria-label]': 'unavailable() ? "Image unavailable" : null',
	},
})
export class MediaThumbnail {
	readonly image = input.required<PickerImage>()

	protected readonly failedUrl = signal<string | null>(null)
	protected readonly unavailable = computed(() => this.failedUrl() === this.image().url)
}
