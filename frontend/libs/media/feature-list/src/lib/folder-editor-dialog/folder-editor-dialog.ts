import { Component, computed, inject, signal } from '@angular/core'
import { disabled, form, FormField, FormRoot, maxLength, required } from '@angular/forms/signals'
import { MediaQueryOptions, type MediaFolder } from '@game-atlas/media/data-access'
import { Button } from '@game-atlas/shared/ui'
import { getDirtyValues } from '@game-atlas/shared/util'
import { BrnDialogRef, injectBrnDialogContext } from '@spartan-ng/brain/dialog'
import { toast } from '@spartan-ng/brain/sonner'
import { HlmFieldImports } from '@spartan-ng/helm/field'
import { HlmInput } from '@spartan-ng/helm/input'
import { injectMutation } from '@tanstack/angular-query-experimental'
import { getMediaErrorMessage } from '../get-media-error-message'

type FolderEditorContext = {
	folder: MediaFolder | null
	parentId: string | null
}

type FolderFormModel = {
	name: string
}

@Component({
	selector: 'media-folder-editor-dialog',
	imports: [Button, HlmInput, FormField, FormRoot, HlmFieldImports],
	templateUrl: './folder-editor-dialog.html',
	host: {
		'[attr.data-state]': 'dialog.state()',
		class: 'block w-[min(28rem,calc(100vw-2rem))] rounded-xl border bg-background p-6 text-foreground shadow-xl',
	},
})
export class FolderEditorDialog {
	protected readonly context = injectBrnDialogContext<FolderEditorContext>()
	protected readonly dialog = inject(BrnDialogRef)
	readonly #mediaQueryOptions = inject(MediaQueryOptions)

	readonly #updateFolderMutation = injectMutation(() => this.#mediaQueryOptions.updateFolder())
	readonly #createFolderMutation = injectMutation(() => this.#mediaQueryOptions.createFolder())

	readonly #model = signal<FolderFormModel>({ name: this.context.folder?.name ?? '' })

	protected readonly folderForm = form(
		this.#model,
		path => {
			disabled(path, {
				when: () => this.#createFolderMutation.isPending() || this.#updateFolderMutation.isPending(),
			})

			required(path.name, { message: 'Name is required.' })
			maxLength(path.name, 200, { message: 'Name must be at most 200 characters.' })
		},
		{
			submission: {
				action: async folderForm => {
					const callbacks = {
						onSuccess: () => {
							folderForm().reset()
							this.dialog.close(true)
						},
						onError: (error: unknown) => toast.error(getMediaErrorMessage(error)),
					}

					if (this.context.folder) {
						const patch = getDirtyValues(folderForm)

						if (!Object.keys(patch).length) return

						await this.#updateFolderMutation
							.mutateAsync({ id: this.context.folder.id, patch }, callbacks)
							.catch(() => undefined)
					} else {
						await this.#createFolderMutation
							.mutateAsync(
								{ name: folderForm.name().value(), parentId: this.context.parentId },
								callbacks,
							)
							.catch(() => undefined)
					}
				},
			},
		},
	)

	protected readonly errorMessage = computed(() => {
		const error = this.context.folder ? this.#updateFolderMutation.error() : this.#createFolderMutation.error()

		return error ? getMediaErrorMessage(error) : ''
	})
}
