import { Component, computed, inject, signal } from '@angular/core'
import { FormsModule } from '@angular/forms'
import { MEDIA_PICKER_SOURCE, type LibraryImage, type LibraryFolder } from '@game-atlas/shared/util'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matFolderRound,
	matSearchRound,
	matCheckCircleRound,
	matCloseRound,
	matChevronLeftRound,
	matChevronRightRound,
} from '@ng-icons/material-symbols/round'
import { HlmSkeleton } from '@spartan-ng/helm/skeleton'
import { Button } from '../button/button'
import { MediaThumbnail } from '../media-thumbnail/media-thumbnail'
import { HlmInput } from '@spartan-ng/helm/input'
import { HlmAttachmentImports } from '@spartan-ng/helm/attachment'
import { BrnDialogRef } from '@spartan-ng/brain/dialog'
import { injectQuery } from '@tanstack/angular-query-experimental'
@Component({
	selector: 'ui-media-picker-dialog',
	imports: [Button, MediaThumbnail, FormsModule, HlmInput, HlmAttachmentImports, NgIcon, HlmSkeleton],
	templateUrl: './media-picker-dialog.html',
	styleUrl: './media-picker-dialog.css',
	providers: [
		provideIcons({
			matFolderRound,
			matSearchRound,
			matCheckCircleRound,
			matCloseRound,
			matChevronLeftRound,
			matChevronRightRound,
		}),
	],
	host: {
		class: 'flex max-h-[90dvh] w-[min(52rem,calc(100vw-2rem))] flex-col overflow-hidden rounded-4xl bg-popover text-popover-foreground ring-1 ring-foreground/5',
	},
})
export class MediaPickerDialog {
	protected readonly dialog = inject(BrnDialogRef)
	readonly #source = inject(MEDIA_PICKER_SOURCE)
	protected readonly offset = signal(0)
	protected readonly search = signal('')
	protected readonly path = signal<LibraryFolder[]>([])
	protected readonly skeletons = Array.from({ length: 12 }, (value, index) => index)
	protected readonly mediaQuery = injectQuery(() =>
		this.#source.list({
			folderId: this.path()[this.path().length - 1]?.id,
			offset: this.offset(),
			limit: 24,
			search: this.search().trim(),
		}),
	)
	protected readonly selectedImage = signal<LibraryImage | null>(null)
	protected readonly getMediaErrorMessage = this.#source.errorMessage
	protected readonly hasChoices = computed(() => !!this.mediaQuery.data()?.items.length)

	protected openFolder(folder: LibraryFolder): void {
		this.path.update(path => [...path, folder])
		this.resetPage()
	}

	protected navigateTo(index: number): void {
		this.path.update(path => path.slice(0, index + 1))
		this.resetPage()
	}

	protected resetPage(): void {
		this.offset.set(0)
		this.search.set('')
		this.selectedImage.set(null)
	}
}
