import type { Map as GameMap, MapStyle } from './generated/models'

export function acceptStyle(current: MapStyle | undefined, incoming: MapStyle): MapStyle {
	if (!current) return incoming

	if (current.updatedAt > incoming.updatedAt) return current

	if (current.updatedAt === incoming.updatedAt) {
		if (current.status === 'ready' || current.status === 'failed') return current

		if (current.status === 'processing' && incoming.status === 'queued') return current
	}

	return current.status === incoming.status && current.status === 'processing'
		? { ...incoming, progress: Math.max(current.progress, incoming.progress) }
		: incoming
}

export function acceptMap(current: GameMap | undefined, incoming: GameMap): GameMap {
	const metadata = current && current.updatedAt > incoming.updatedAt ? current : incoming
	// A late response must not undo a successfully persisted display order or membership.
	const styles = metadata.styles.map(style =>
		acceptStyle(
			current?.styles.find(item => item.id === style.id),
			incoming.styles.find(item => item.id === style.id) ?? style,
		),
	)

	return { ...metadata, styles }
}

export function withStyle(map: GameMap, styleId: string, change: (style: MapStyle) => MapStyle | undefined): GameMap {
	const styles = map.styles.flatMap(style => {
		const next = style.id === styleId ? change(style) : style

		return next ? [next] : []
	})

	return {
		...map,
		styles,
		defaultStyleId:
			styles.find(style => style.id === map.defaultStyleId && style.status === 'ready')?.id ??
			styles.find(style => style.status === 'ready' && style.tilesUrlTemplate)?.id ??
			null,
	}
}
