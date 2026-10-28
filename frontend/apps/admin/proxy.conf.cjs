module.exports = {
	'/media/files/**': {
		target: 'http://localhost:5041',
		changeOrigin: true,
	},
	'/api/**': {
		target: 'http://localhost:5041',
		changeOrigin: true,
		pathRewrite: { '^/api': '' },
	},
}
