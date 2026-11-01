module.exports = {
	'/map-tiles/**': {
		target: 'http://localhost:5041',
		changeOrigin: true,
	},
	'/media/files/**': {
		target: 'http://localhost:5041',
		changeOrigin: true,
	},
	'/api/**': {
		target: 'http://localhost:5041',
		ws: true,
		changeOrigin: true,
		pathRewrite: { '^/api': '' },
	},
}
