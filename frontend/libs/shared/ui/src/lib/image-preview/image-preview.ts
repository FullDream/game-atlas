import { Component, input } from '@angular/core'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matMapRound } from '@ng-icons/material-symbols/round'
import { MediaThumbnail, type PickerImage } from '../media-thumbnail/media-thumbnail'

@Component({
	selector: 'ui-image-preview',
	imports: [MediaThumbnail, NgIcon],
	templateUrl: './image-preview.html',
	providers: [provideIcons({ matMapRound })],
	host: { class: 'block overflow-hidden bg-muted' },
})
export class ImagePreview {
	readonly image = input<PickerImage | null>(null)
}
