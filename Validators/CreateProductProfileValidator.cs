using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AdvancedNetExercise.Features.Products;
using AdvancedNetExercise.Features.Products.DTOs;
using AdvancedNetExercise.Common.Logging;
using Microsoft.Extensions.Logging;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
public class ApplicationContext
{
    
    public DbSet<Product> Products { get; set; } = new MockDbSet<Product>();
}

public class MockDbSet<T> : List<T>, DbSet<T> where T : class
{
    public IQueryable<T> AsQueryable() => this.AsQueryable();
    public Task<T?> FindAsync(object?[]? keyValues, CancellationToken cancellationToken) => Task.FromResult<T?>(null);
    public T Add(T entity) { base.Add(entity); return entity; }
    public void Remove(T entity) { base.Remove(entity); }
    public Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<IServiceProvider> GetInfrastructure() => throw new NotImplementedException();
    public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T> Attach(T entity) => throw new NotImplementedException();
    public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T> Update(T entity) => throw new NotImplementedException();
    public Microsoft.EntityFrameworkCore.Metadata.IEntityType EntityType => throw new NotImplementedException();
    public new System.Collections.Generic.IEnumerator<T> GetEnumerator() => base.GetEnumerator();
    IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken) => throw new NotImplementedException();
}