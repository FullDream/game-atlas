using GameAtlas.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
	: DbContext(options), IApplicationDbContext;
