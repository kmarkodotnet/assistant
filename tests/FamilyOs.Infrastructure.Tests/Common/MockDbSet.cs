using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using System.Linq.Expressions;

namespace FamilyOs.Infrastructure.Tests.Common;

/// <summary>
/// Creates a NSubstitute-backed DbSet&lt;T&gt; that supports LINQ-to-Objects queries,
/// AsNoTracking(), and FirstOrDefaultAsync()/ToHashSetAsync() via the EF Core
/// async provider bridge.
/// </summary>
public static class MockDbSet
{
    public static DbSet<T> Create<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var dbSet = Substitute.For<DbSet<T>, IQueryable<T>, IAsyncEnumerable<T>>();

        // IQueryable wiring
        ((IQueryable<T>)dbSet).Provider.Returns(
            new TestAsyncQueryProvider<T>(queryable.Provider));
        ((IQueryable<T>)dbSet).Expression.Returns(queryable.Expression);
        ((IQueryable<T>)dbSet).ElementType.Returns(queryable.ElementType);
        ((IQueryable<T>)dbSet).GetEnumerator().Returns(queryable.GetEnumerator());

        // IAsyncEnumerable wiring
        ((IAsyncEnumerable<T>)dbSet)
            .GetAsyncEnumerator(Arg.Any<CancellationToken>())
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        return dbSet;
    }

    // ----- Async provider wrappers ----------------------------------------

    private sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

        public IQueryable CreateQuery(Expression expression)
            => new TestAsyncEnumerable<TEntity>(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new TestAsyncEnumerable<TElement>(expression);

        public object? Execute(Expression expression) => _inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            // TResult is Task<U>; extract U and execute synchronously, then wrap in Task.FromResult<U>
            var innerType = typeof(TResult).GetGenericArguments()[0];
            var result = _inner.Execute(expression); // returns object? evaluated by LINQ-to-Objects
            return (TResult)typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(innerType)
                .Invoke(null, [result])!;
        }
    }

    private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    private sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

        public T Current => _inner.Current;

        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
