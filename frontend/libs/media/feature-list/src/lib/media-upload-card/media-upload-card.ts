import { Component, computed, input, output } from '@angular/core'
import { toObservable, toSignal } from '@angular/core/rxjs-interop'
import { type MediaUploadMutation } from '@game-atlas/media/data-access'
import { Button, TooltipOnOverflow } from '@game-atlas/shared/ui'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matCloseRound, matErrorRound, matImageRound, matRefreshRound } from '@ng-icons/material-symbols/round'
import { HlmProgressImports } from '@spartan-ng/helm/progress'
import { HlmSpinner } from '@spartan-ng/helm/spinner'
import { HlmTooltip } from '@spartan-ng/helm/tooltip'
import { distinctUntilChanged, map, switchMap } from 'rxjs'
import { getMediaErrorMessage } from '../get-media-error-message'
import { mediaUploadPreview } from './media-upload-preview'

@Component({
	selector: 'media-upload-card',
	imports: [TooltipOnOverflow, Button, NgIcon, HlmSpinner, HlmTooltip, HlmProgressImports],
	templateUrl: './media-upload-card.html',
	providers: [provideIcons({ matImageRound, matRefreshRound, matCloseRound, matErrorRound })],
	host: {
		role: 'article',
		class: 'block min-w-0 overflow-hidden rounded-lg border bg-card',
		'[attr.aria-label]': '"Upload " + upload().variables.file.name',
	},
})
export class MediaUploadCard {
	readonly upload = input.required<MediaUploadMutation>()
	readonly percent = input<number | null>(0)

	readonly retry = output()
	readonly removed = output()

	protected readonly progressLabel = (value: number): string =>
		this.upload().status === 'error' ? 'Upload failed' : value + '%'

	protected readonly preview = toSignal(
		toObservable(this.upload).pipe(
			map(upload => upload.variables.file),
			distinctUntilChanged(),
			switchMap(mediaUploadPreview),
		),
		{ initialValue: null },
	)

	protected readonly error = computed(() =>
		this.upload().status === 'error' ? getMediaErrorMessage(this.upload().error) : '',
	)

	protected readonly indeterminate = computed(() => this.percent() === null || this.percent() === 100)

	protected readonly progressValue = computed(() => {
		if (this.upload().status === 'error') return 100

		return this.indeterminate() ? null : this.percent()
	})

	protected readonly progressVariant = computed(() => {
		if (this.upload().status === 'error') return 'destructive'

		return this.indeterminate() ? 'muted' : 'default'
	})

	protected readonly status = computed(() => {
		if (this.upload().status === 'error') return 'Upload failed'

		return this.percent() === 100 ? 'Processing' : 'Uploading'
	})
}
