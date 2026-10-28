import { DatePipe, DecimalPipe } from '@angular/common'
import { Component, computed, DestroyRef, inject, input, linkedSignal, type Signal } from '@angular/core'
import { takeUntilDestroyed } from '@angular/core/rxjs-interop'
import { disabled, form, FormField, FormRoot, maxLength, required } from '@angular/forms/signals'
import { ActivatedRoute, Router, ROUTER_OUTLET_DATA } from '@angular/router'
import {
	MediaQueryOptions,
	type ImageAssetDto,
	type MediaAssetDto,
	type PaginatedResultOfMediaItemDto,
} from '@game-atlas/media/data-access'
import { Button, PendingChanges } from '@game-atlas/shared/ui'
import { getDirtyValues } from '@game-atlas/shared/util'
import { BrnDialogService } from '@spartan-ng/brain/dialog'
import { toast } from '@spartan-ng/brain/sonner'
import { HlmFieldImports } from '@spartan-ng/helm/field'
import { HlmInput } from '@spartan-ng/helm/input'
import { HlmSheetImports } from '@spartan-ng/helm/sheet'
import { HlmSpinner } from '@spartan-ng/helm/spinner'
import { injectMutation } from '@tanstack/angular-query-experimental'
import { defaultIfEmpty, finalize, map, Observable, of, shareReplay, take, tap } from 'rxjs'
import { ConfirmationDialog } from '../confirmation-dialog/confirmation-dialog'
import { getMediaErrorMessage } from '../get-media-error-message'
import { MediaPickerDialog } from '../media-picker-dialog/media-picker-dialog'
import { MediaThumbnail } from '../media-thumbnail/media-thumbnail'
import { MediaDetailsDismiss } from './media-details-dismiss'

type MediaDetailsOutletData = {
	listing: PaginatedResultOfMediaItemDto | undefined
	loading: boolean
	error: unknown
}

type MediaFormModel = {
	name: string
	alt: string
	previewAssetId: string | null
}

@Component({
	selector: 'media-details',
	imports: [
		HlmSpinner,
		MediaDetailsDismiss,
		Button,
		HlmInput,
		FormField,
		FormRoot,
		HlmFieldImports,
		MediaThumbnail,
		DatePipe,
		DecimalPipe,
		HlmSheetImports,
	],
	templateUrl: './media-details.html',
	host: {
		class: 'contents',
		'aria-label': 'Media details',
	},
})
export class MediaDetails {
	readonly id = input.required<string>()

	protected readonly outletData = inject(ROUTER_OUTLET_DATA) as Signal<MediaDetailsOutletData>

	readonly #activatedRoute = inject(ActivatedRoute)
	readonly #router = inject(Router)
	readonly #mediaQueryOptions = inject(MediaQueryOptions)
	readonly #dialogService = inject(BrnDialogService)
	readonly #destroyRef = inject(DestroyRef)
	readonly #pendingChanges = inject(PendingChanges)

	readonly #updateAssetMutation = injectMutation(() => this.#mediaQueryOptions.updateAsset())

	#confirmation: Observable<boolean> | null = null

	protected readonly selectedAsset = linkedSignal<
		{ id: string; listing: PaginatedResultOfMediaItemDto | undefined },
		MediaAssetDto | null
	>({
		source: () => ({ id: this.id(), listing: this.outletData().listing }),
		computation: ({ id, listing }, previous) => {
			if (previous?.source.id === id && previous.value) return previous.value

			const item = listing?.items.find(item => item.id === id)

			return item && item.type !== 'folder' ? item : null
		},
	})

	readonly #model = linkedSignal(() => this.#createFormModel(this.selectedAsset()))

	protected readonly preview = linkedSignal(() => {
		const asset = this.selectedAsset()

		return asset?.type === 'video' ? asset.preview : null
	})

	protected readonly mediaForm = form(
		this.#model,
		path => {
			disabled(path, { when: () => this.#updateAssetMutation.isPending() })
			required(path.name, { message: 'Name is required.' })
			maxLength(path.name, 200, { message: 'Name must be at most 200 characters.' })
		},
		{
			submission: {
				action: async mediaForm => {
					const asset = this.selectedAsset()
					const patch = getDirtyValues(mediaForm)

					if (!asset || !Object.keys(patch).length) return

					await this.#updateAssetMutation
						.mutateAsync(
							{ id: asset.id, patch },
							{
								onSuccess: asset => {
									this.selectedAsset.set(asset)
									mediaForm().reset()
								},
								onError: error => toast.error(getMediaErrorMessage(error)),
							},
						)
						.catch(() => undefined)
				},
			},
		},
	)

	protected readonly errorMessage = computed(() => {
		const error = this.#updateAssetMutation.error()

		return error && this.#updateAssetMutation.variables()?.id === this.selectedAsset()?.id
			? getMediaErrorMessage(error)
			: ''
	})

	protected readonly videoError = linkedSignal({ source: this.selectedAsset, computation: () => false })

	protected readonly poster = computed(() => this.preview()?.url ?? '')

	constructor() {
		this.#pendingChanges.check = () => this.canLeave()

		this.#destroyRef.onDestroy(() => {
			this.#pendingChanges.check = () => of(true)
		})
	}

	canLeave(): Observable<boolean> {
		if (this.mediaForm().submitting()) {
			return of(false)
		}

		if (!this.mediaForm().dirty()) {
			return of(true)
		}

		if (this.#confirmation) {
			return this.#confirmation
		}

		const dialog = this.#dialogService.open(
			ConfirmationDialog,
			undefined,
			{
				title: 'Discard changes?',
				description: 'Your unsaved changes will be lost.',
				confirm: 'Discard changes',
				cancel: 'Continue editing',
				destructive: true,
			},
			{ ariaLabel: 'Discard changes?', role: 'alertdialog', autoFocus: 'first-tabbable' },
		)

		this.#confirmation = dialog.closed$.pipe(
			take(1),
			takeUntilDestroyed(this.#destroyRef),
			map(result => result === true),
			tap(allowed => {
				if (!allowed) return

				const asset = this.selectedAsset()

				this.preview.set(asset?.type === 'video' ? asset.preview : null)

				this.#model.set(this.#createFormModel(asset))

				this.mediaForm().reset()
			}),
			defaultIfEmpty(false),
			finalize(() => {
				this.#confirmation = null
			}),
			shareReplay({ bufferSize: 1, refCount: false }),
		)

		return this.#confirmation
	}

	protected requestClose(): void {
		void this.#router.navigate([{ outlets: { details: null } }], {
			relativeTo: this.#activatedRoute.parent,
			queryParamsHandling: 'preserve',
			replaceUrl: true,
		})
	}

	protected choosePreview(image: ImageAssetDto | null): void {
		this.preview.set(image)
		this.#model.update(draft => ({ ...draft, previewAssetId: image?.id ?? null }))
		this.mediaForm.previewAssetId().markAsDirty()
	}

	protected chooseCover(): void {
		const dialog = this.#dialogService.open<{ mode: 'image'; folderId: string | null }, ImageAssetDto>(
			MediaPickerDialog,
			undefined,
			{ mode: 'image', folderId: this.selectedAsset()?.folderId ?? null },
			{ ariaLabel: 'Choose a video cover' },
		)

		dialog.closed$.pipe(take(1), takeUntilDestroyed(this.#destroyRef)).subscribe(image => {
			if (image) this.choosePreview(image)
		})
	}

	#createFormModel(asset: MediaAssetDto | null): MediaFormModel {
		return {
			name: asset?.name ?? '',
			alt: asset?.type === 'image' ? asset.alt : '',
			previewAssetId: asset?.type === 'video' ? (asset.preview?.id ?? null) : null,
		}
	}
}
