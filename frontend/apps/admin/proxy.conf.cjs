module.exports = {
	'/api/**': {
		target: 'http://localhost:5041',
		changeOrigin: true,
		pathRewrite: { '^/api': '' },
	},
}
