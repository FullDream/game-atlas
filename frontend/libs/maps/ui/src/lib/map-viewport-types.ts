export type MapGeometry = {
	id: string
	extent: number[] | null
	tileOrigin: number[] | null
	tileSize: number | null
	resolutions: number[] | null
	minZoom: number | null
	maxZoom: number | null
}

export type MapPoint = { x: number; y: number }
export type MapTool = 'pan' | 'move' | 'add'

export type MapMarker = {
	id: string
	name: string
	position: MapPoint
	color: string | null
	icon: string | null
	selected?: boolean
	pending?: boolean
	failed?: boolean
}

export type MarkerMove = { id: string; position: MapPoint }
export type VisibleMapRegion = { left: number; top: number; right: number; bottom: number }

export type MarkerInteractions = {
	select: (id: string) => void
	place: (position: MapPoint) => void
	move: (movement: MarkerMove) => void
}

export type MapViewStyle = {
	id: string
	name: string
	backgroundColor: string
	status: string
	tilesUrlTemplate: string | null
	tileGeometry: TileGeometry
	source: MapStyleSource
}

export type MapStyleSource = {
	id: string
	name: string
	url: string
	alt: string
	placeholder: string | null
}

export type TileGeometry = {
	extent: number[]
	tileOrigin: number[]
	tileSize: number
	resolutions: number[]
	minZoom: number
	maxZoom: number
}
