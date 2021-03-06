﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.EntityClient;
using System.Data.Objects;
using System.Linq;
using Highway.Data.Interceptors.Events;
using Highway.Data.Interfaces;

namespace Highway.Data
{
    /// <summary>
    /// The default implementation of a Object context for the database first approach to Entity Framework
    /// </summary>
    [Obsolete(
        "This is prototype code to try and support the database first approach. It is not guaranteed that this will be supported or further developed going forward as we currently see it as a sub-optimal approach."
        , false)]
    public class ObjectDataContext : ObjectContext, IObservableDataContext
    {
        private IEventManager _eventManager;

        /// <summary>
        /// Creates a database or model first context        /// </summary>
        /// <param name="connection"></param>
        public ObjectDataContext(EntityConnection connection) : base(connection)
        {
        }

        /// <summary>
        /// Creates a database or model first context
        /// </summary>
        /// <param name="connectionString"></param>
        public ObjectDataContext(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Creates a database or model first context
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="defaultContainerName"></param>
        protected ObjectDataContext(string connectionString, string defaultContainerName)
            : base(connectionString, defaultContainerName)
        {
        }

        /// <summary>
        /// Creates a database or model first context
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="defaultContainerName"></param>
        protected ObjectDataContext(EntityConnection connection, string defaultContainerName)
            : base(connection, defaultContainerName)
        {
        }

        #region IObservableDataContext Members

        /// <summary>
        /// This gives a mockable wrapper around the normal Set<typeparamref name="T"/> method that allows for testablity
        /// </summary>
        /// <typeparam name="T">The Entity being queried</typeparam>
        /// <returns><see cref="IQueryable{T}"/></returns>
        public IQueryable<T> AsQueryable<T>() where T : class
        {
            return CreateObjectSet<T>();
        }

        /// <summary>
        /// Adds the provided instance of <typeparamref name="T"/> to the data context
        /// </summary>
        /// <typeparam name="T">The Entity Type being added</typeparam>
        /// <param name="item">The <typeparamref name="T"/> you want to add</param>
        /// <returns>The <typeparamref name="T"/> you added</returns>
        public T Add<T>(T item) where T : class
        {
            CreateObjectSet<T>().AddObject(item);
            return item;
        }

        /// <summary>
        /// Removes the provided instance of <typeparamref name="T"/> from the data context
        /// </summary>
        /// <typeparam name="T">The Entity Type being removed</typeparam>
        /// <param name="item">The <typeparamref name="T"/> you want to remove</param>
        /// <returns>The <typeparamref name="T"/> you removed</returns>
        public T Remove<T>(T item) where T : class
        {
            DeleteObject(item);
            return item;
        }

        /// <summary>
        /// Updates the provided instance of <typeparamref name="T"/> in the data context
        /// </summary>
        /// <typeparam name="T">The Entity Type being updated</typeparam>
        /// <param name="item">The <typeparamref name="T"/> you want to update</param>
        /// <returns>The <typeparamref name="T"/> you updated</returns>
        public T Update<T>(T item) where T : class
        {
            ObjectStateEntry entry = ObjectStateManager.GetObjectStateEntry(item);
            if (entry != null)
            {
                entry.ApplyCurrentValues(item);
            }
            else
            {
                Attach(item);
                entry = ObjectStateManager.GetObjectStateEntry(item);
                entry.SetModified();
            }
            return item;
        }

        /// <summary>
        /// Attaches the provided instance of <typeparamref name="T"/> to the data context
        /// </summary>
        /// <typeparam name="T">The Entity Type being attached</typeparam>
        /// <param name="item">The <typeparamref name="T"/> you want to attach</param>
        /// <returns>The <typeparamref name="T"/> you attached</returns>
        public T Attach<T>(T item) where T : class
        {
            CreateObjectSet<T>().Attach(item);
            return item;
        }

        /// <summary>
        /// Detaches the provided instance of <typeparamref name="T"/> from the data context
        /// </summary>
        /// <typeparam name="T">The Entity Type being detached</typeparam>
        /// <param name="item">The <typeparamref name="T"/> you want to detach</param>
        /// <returns>The <typeparamref name="T"/> you detached</returns>
        public T Detach<T>(T item) where T : class
        {
            CreateObjectSet<T>().Detach(item);
            return item;
        }

        /// <summary>
        /// Reloads the provided instance of <typeparamref name="T"/> from the database
        /// </summary>
        /// <typeparam name="T">The Entity Type being reloaded</typeparam>
        /// <param name="item">The <typeparamref name="T"/> you want to reload</param>
        /// <returns>The <typeparamref name="T"/> you reloaded</returns>
        public T Reload<T>(T item) where T : class
        {
            ObjectStateEntry entry = ObjectStateManager.GetObjectStateEntry(item);
            if (entry != null)
            {
                Refresh(RefreshMode.StoreWins, item);
            }
            return item;
        }

        /// <summary>
        /// Commits all currently tracked entity changes
        /// </summary>
        /// <returns>the number of rows affected</returns>
        public int Commit()
        {
            base.DetectChanges();
            InvokePreSave();
            int result = base.SaveChanges();
            InvokePostSave();
            return result;
        }

        /// <summary>
        /// Executes a SQL command and tries to map the returned datasets into an <see cref="IEnumerable{T}"/>
        /// The results should have the same column names as the Entity Type has properties
        /// </summary>
        /// <typeparam name="T">The Entity Type that the return should be mapped to</typeparam>
        /// <param name="sql">The Sql Statement</param>
        /// <param name="dbParams">A List of Database Parameters for the Query</param>
        /// <returns>An <see cref="IEnumerable{T}"/> from the query return</returns>
        public IEnumerable<T> ExecuteSqlQuery<T>(string sql, params DbParameter[] dbParams)
        {
            ObjectParameter[] objectParameters =
                dbParams.Select(x => new ObjectParameter(x.ParameterName, x.Value)).ToArray();
            return CreateQuery<T>(sql, objectParameters);
        }

        /// <summary>
        /// Executes a SQL command and returns the standard int return from the query
        /// </summary>
        /// <param name="sql">The Sql Statement</param>
        /// <param name="dbParams">A List of Database Parameters for the Query</param>
        /// <returns>The rows affected</returns>
        public int ExecuteSqlCommand(string sql, params DbParameter[] dbParams)
        {
            return ExecuteSqlQuery<int>(sql, dbParams).FirstOrDefault();
        }

        /// <summary>
        /// The reference to EventManager that allows for ordered event handling and registration
        /// </summary>
        public IEventManager EventManager
        {
            get { return _eventManager; }
            set
            {
                _eventManager = value;
                _eventManager.Context = this;
            }
        }

        /// <summary>
        /// The event fired just before the commit of the ORM
        /// </summary>
        public event EventHandler<PreSaveEventArgs> PreSave;

        /// <summary>
        /// The event fired just after the commit of the ORM
        /// </summary>
        public event EventHandler<PostSaveEventArgs> PostSave;

        #endregion

        private void InvokePostSave()
        {
            if (PostSave != null) PostSave(this, new PostSaveEventArgs());
        }

        private void InvokePreSave()
        {
            if (PreSave != null) PreSave(this, new PreSaveEventArgs());
        }
    }
}