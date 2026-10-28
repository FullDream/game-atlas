import { NgOptimizedImage } from '@angular/common'
import { Component, computed, input, signal } from '@angular/core'
import type { ImageAssetDto } from '@game-atlas/media/data-access'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matBrokenImageRound } from '@ng-icons/material-symbols/round'

@Component({
	selector: 'media-thumbnail',
	imports: [NgOptimizedImage, NgIcon],
	templateUrl: './media-thumbnail.html',
	providers: [provideIcons({ matBrokenImageRound })],
	host: {
		class: 'relative block overflow-hidden bg-muted',
		'[attr.aria-label]': 'unavailable() ? "Image unavailable" : null',
	},
})
export class MediaThumbnail {
	readonly image = input.required<ImageAssetDto>()

	protected readonly failedUrl = signal<string | null>(null)
	protected readonly unavailable = computed(() => this.failedUrl() === this.image().url)
}
