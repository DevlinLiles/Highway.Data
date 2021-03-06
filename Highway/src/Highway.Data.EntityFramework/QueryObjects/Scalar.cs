﻿using System;
using Highway.Data.Interfaces;

namespace Highway.Data.QueryObjects
{
    /// <summary>
    /// Base implementation of a query that returns a single value or object
    /// </summary>
    /// <typeparam name="T">The type of object or value being returned</typeparam>
    public class Scalar<T> : QueryBase, IScalar<T>
    {
        /// <summary>
        /// The query to be executed later
        /// </summary>
        protected Func<IDataContext, T> ContextQuery { get; set; }

        #region IScalar<T> Members

        /// <summary>
        /// Executes the expression against the passed in context
        /// </summary>
        /// <param name="context">The data context that the scalar query is executed against</param>
        /// <returns>The instance of <typeparamref name="T"/> that the query materialized if any</returns>
        public T Execute(IDataContext context)
        {
            Context = context;
            CheckContextAndQuery(ContextQuery);
            return ContextQuery(context);
        }

        #endregion
    }
}