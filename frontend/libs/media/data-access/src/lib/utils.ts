import { flatMap, map, pipe, sortBy, uniqueBy } from 'remeda'
import type {
	ListMediaParams,
	MediaItemDto,
	PaginatedResultOfMediaItemDto,
	MediaFolderDetailsDto,
} from './generated/models'

const encoder = new TextEncoder()

export function mediaParent(item: MediaItemDto): string | null {
	return item.type === 'folder' ? item.parentId : item.folderId
}

export function reconcileMediaList(
	data: PaginatedResultOfMediaItemDto,
	params: ListMediaParams,
	previous: MediaItemDto | null,
	next: MediaItemDto | null,
): { data: PaginatedResultOfMediaItemDto; invalidate: boolean } {
	const entity = next ?? previous

	if (!entity) return { data, invalidate: false }

	const parent = params.folderId ?? null
	const oldParent = previous ? mediaParent(previous) : undefined
	const newParent = next ? mediaParent(next) : undefined
	const delta = Number(newParent === parent) - Number(oldParent === parent)
	const changed = !previous || !next || previous.name !== next.name || oldParent !== newParent
	const complete =
		!params.search && data.offset === 0 && data.items.length === data.total && data.total + delta <= data.limit
	const items = pipe(
		data.items,
		flatMap((item): MediaItemDto[] => {
			if (item.id === entity.id) return next && newParent === parent ? [next] : []

			if (next?.type === 'image' && item.type === 'video' && item.preview?.id === next.id)
				return [{ ...item, preview: next }]

			return [item]
		}),
		items =>
			changed && complete && next && newParent === parent ? uniqueBy([...items, next], item => item.id) : items,
		items =>
			changed && complete
				? sortBy(
						items,
						item => Number(item.type !== 'folder'),
						// Match SQLite BINARY's UTF-8 byte order rather than locale or UTF-16 ordering.
						item => String.fromCharCode(...encoder.encode(item.name)),
						item => item.id,
					)
				: items,
		map(item =>
			item.type === 'folder'
				? {
						...item,
						itemsCount: item.itemsCount + Number(newParent === item.id) - Number(oldParent === item.id),
					}
				: item,
		),
	)

	return {
		data: {
			...data,
			items,
			total: !params.search && changed ? Math.max(0, data.total + delta) : data.total,
		},
		invalidate: changed && (oldParent === parent || newParent === parent) && !complete,
	}
}

export function reconcileMediaFolder(
	data: MediaFolderDetailsDto,
	previous: MediaItemDto | null,
	next: MediaItemDto | null,
): { data: MediaFolderDetailsDto; invalidate: boolean } {
	const entity = next ?? previous
	const delta =
		Number(!!next && mediaParent(next) === data.folder.id) -
		Number(!!previous && mediaParent(previous) === data.folder.id)
	const isCurrent = entity?.id === data.folder.id
	const inPath = isCurrent || data.ancestors.some(parent => parent.id === entity?.id)
	const hierarchyChanged =
		entity?.type === 'folder' && inPath && (!next || (!!previous && mediaParent(previous) !== mediaParent(next)))

	return {
		data: {
			folder:
				isCurrent && next?.type === 'folder'
					? next
					: { ...data.folder, itemsCount: data.folder.itemsCount + delta },
			ancestors: data.ancestors.map(parent =>
				next?.type === 'folder' && parent.id === next.id ? { id: next.id, name: next.name } : parent,
			),
		},
		invalidate: hierarchyChanged,
	}
}
