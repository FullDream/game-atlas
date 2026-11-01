export type MapGeometry = {
	id: string
	extent: number[] | null
	tileOrigin: number[] | null
	tileSize: number | null
	resolutions: number[] | null
	minZoom: number | null
	maxZoom: number | null
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
