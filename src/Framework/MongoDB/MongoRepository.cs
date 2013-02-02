#region license

// Copyright (c) 2005 - 2007 Ayende Rahien (ayende@ayende.com)
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice,
//     this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice,
//     this list of conditions and the following disclaimer in the documentation
//     and/or other materials provided with the distribution.
//     * Neither the name of Ayende Rahien nor the names of its
//     contributors may be used to endorse or promote products derived from this
//     software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
// THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using N2.Engine;
using _MongoDB = MongoDB;
using N2.Details;
using N2.Security;
using N2.Edit.Versioning;
using MongoDB.Bson;
using System.Linq;
using System.Collections;

namespace N2.Persistence.MongoDB
{
    [Service]
    [Service(typeof(IRepository<>),
		Configuration = "mongo",
		Replaces = typeof(NH.NHRepository<>))]
    public class MongoDbRepository<TEntity> : IRepository<TEntity> where TEntity : class
    {
		private MongoDatabaseProvider provider;
		private System.Reflection.PropertyInfo idGetter;

		public MongoDatabaseProvider Provider
		{
			get { return provider; }
		}

        public MongoDbRepository(MongoDatabaseProvider provider)
        {
			this.provider = provider;
			idGetter = typeof(TEntity).GetProperty("ID");
        }

        public TEntity Load(object id)
        {
			return Get(id);
        }

        public void Save(TEntity entity)
        {
            var col = GetCollection();
            col.Save(entity);
        }

        public void Update(TEntity entity)
        {
			SaveOrUpdate(entity);
        }

        public TEntity Get(object id)
        {
            var col = GetCollection();
            var result = col.FindOne(Query.EQ("_id", (int)id));
            return result;
        }

        protected MongoCollection<TEntity> GetCollection()
        {
            return provider.Database.GetCollection<TEntity>(typeof(TEntity).Name);
        }

        public IEnumerable<TEntity> Find(string propertyName, object value)
        {
			return Find(Parameter.Equal(propertyName, value));
        }

        public IEnumerable<TEntity> Find(params Parameter[] propertyValuesToMatchAll)
        {
			var collection = GetCollection();
			if (propertyValuesToMatchAll.Length > 1)
				return collection.Find(new ParameterCollection(propertyValuesToMatchAll).CreateQuery());
			else if (propertyValuesToMatchAll.Length == 1)
				return collection.Find(propertyValuesToMatchAll[0].CreateQuery());
			else
				return collection.FindAll();
		}

        public IEnumerable<TEntity> Find(IParameter parameters)
        {
			return GetCollection().Find(parameters.CreateQuery());
        }

        public IEnumerable<IDictionary<string, object>> Select(IParameter parameters, params string[] properties)
        {
			var map = "function() { emit(this._id, { " + string.Join(", ", properties.Select(p => p == "ID" ? "_id" : p).Select(p => "'" + p + "': this['" + p + "']")) +  " }); }";
			var reduce = "function(key, emits) { return emits[0]; }";
			var finalize = "";
			var result = GetCollection().MapReduce(parameters.CreateQuery(), map, reduce);//, new MapReduceOptionsBuilder().SetFinalize(finalize));
			throw new NotSupportedException();
        }

        public void Delete(TEntity entity)
        {
			var idValue = GetEntityId(entity);
            GetCollection().Remove(Query.EQ("_id", idValue));
        }

		protected virtual int GetEntityId(TEntity entity)
		{
			return (int)idGetter.GetValue(entity, null);
		}

        public void SaveOrUpdate(TEntity entity)
		{
			var idValue = GetEntityId(entity);

			if (idValue != 0)
			{
				var update = _MongoDB.Driver.Builders.Update.Replace(entity);
				var result = GetCollection().Update(
					Query.EQ("_id", idValue),
					update,
					UpdateFlags.Upsert,
					WriteConcern.Acknowledged);
			}
			else
			{
				Save(entity);
			}
        }

        public bool Exists()
        {
			return Count() > 0;
        }

        public long Count()
        {
			return GetCollection().Count();
        }

        public long Count(IParameter parameters)
        {
			return GetCollection().Count(parameters.CreateQuery());
        }

        public void Flush()
        {
            //not required for mongodb
        }

		class NullTransaction : ITransaction
		{
			public void Commit()
			{
				if (Committed != null)
					Committed(this, new EventArgs());
			}

			public void Rollback()
			{
				if (Rollbacked != null)
					Rollbacked(this, new EventArgs());
			}

			public event EventHandler Committed;

			public event EventHandler Rollbacked;

			public event EventHandler Disposed;

			public void Dispose()
			{
				if (Disposed != null)
					Disposed(this, new EventArgs());
			}
		}

        public ITransaction BeginTransaction()
        {
			return new NullTransaction();
        }

        public ITransaction GetTransaction()
        {
			return new NullTransaction();
        }

        public void Dispose()
        {
        }
    }

	internal static class MongoRepositoryExtensions
	{
		public static IMongoQuery CreateQuery(this IParameter parameter)
		{
			if (parameter is ParameterCollection)
			{
				var pc = parameter as ParameterCollection;
				switch (pc.Operator)
				{
					case Operator.And:
						return Query.And(pc.Select(p => p.CreateQuery()));
					case Operator.Or:
						return Query.Or(pc.Select(p => p.CreateQuery()));
					case Operator.None:
					default:
						throw new NotSupportedException();
				}
			}
			else if (parameter is Parameter)
			{
				var p = (Parameter)parameter;

				if (p.IsDetail)
				{
					var detailExpression = GetValueExpression("Details." + ContentDetail.GetAssociatedPropertyName(p.Value), p.Comparison, p.Value);
					var collectionExpression = GetValueExpression("DetailCollections.Details." + ContentDetail.GetAssociatedPropertyName(p.Value), p.Comparison, p.Value);

					if (p.Name == null)
						return Query.Or(detailExpression, collectionExpression);
					else
						return Query.Or(
							Query.And(
								detailExpression,
								Query.EQ("Detail.Name", p.Name)),
							Query.And(
								detailExpression,
								Query.EQ("DetailCollections.Detail.Name", p.Name)));
				}

				if (p.Name == "ID")
					p.Name = "_id";
				else if (p.Name == "class")
					p.Name = "_t";
				else if (p.Name == "Parent")
				{
					p.Comparison = Comparison.Equal;
					p.Name = "AncestralTrail";
					p.Value = ((ContentItem)p.Value).GetTrail();
				}

				return GetValueExpression(p.Name, p.Comparison, p.Value);
			}
			throw new NotSupportedException();
		}

		private static IMongoQuery GetValueExpression(string name, Comparison comparison, object value)
		{
			switch (comparison)
			{
				case Comparison.Equal:
					return Query.EQ(name, CreateValue(value));
				case Comparison.GreaterOrEqual:
					return Query.GTE(name, CreateValue(value));
				case Comparison.GreaterThan:
					return Query.GT(name, CreateValue(value));
				case Comparison.LessOrEqual:
					return Query.LTE(name, CreateValue(value));
				case Comparison.LessThan:
					return Query.LT(name, CreateValue(value));
				case Comparison.Like:
					return Query.Matches(name, value.ToString().Replace("%", ".*").Replace("/", "\\/"));
				case Comparison.NotEqual:
					return Query.Not(Query.EQ(name, CreateValue(value)));
				case Comparison.NotLike:
					return Query.Not(Query.Matches(name, value.ToString().Replace("%", ".*")));
				case Comparison.NotNull:
					return Query.Exists(name);
				case Comparison.Null:
					return Query.NotExists(name);
				case Comparison.In:
					return Query.In(name, ((IEnumerable)value).OfType<object>().Select(v => CreateValue(v)));
				case Comparison.NotIn:
					return Query.Not(Query.In(name, ((IEnumerable)value).OfType<object>().Select(v => CreateValue(v))));
				default:
					throw new NotSupportedException();
			}
		}

		private static BsonValue CreateValue(object value)
		{
			if (value is ContentItem)
				return BsonValue.Create(((ContentItem)value).ID);
			return BsonValue.Create(value);
		}
	}
}
