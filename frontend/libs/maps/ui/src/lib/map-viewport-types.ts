export type MapGeometry = {
	id: string
	extent: number[]
	tileOrigin: number[]
	tileSize: number
	resolutions: number[]
	minZoom: number
	maxZoom: number
}

export type MapViewStyle = {
	id: string
	name: string
	backgroundColor: string
	status: string
	tilesUrlTemplate: string | null
	source: MapStyleSource
}

export type MapStyleSource = {
	id: string
	name: string
	url: string
	alt: string
	placeholder: string | null
}
