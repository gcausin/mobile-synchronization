﻿using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SQLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Model.Base
{
    public class AbstractEntity : IUpsertable
    {
        public AbstractEntity()
        {
            ModifiedDate = DateTime.Now.ToUniversalTime();
        }

        private string pk;
        [PrimaryKey, MaxLength(36)]
        public string Pk { get { return pk ?? (pk = Guid.NewGuid().ToString()); } set { pk = value; } }

        [NotNull]
        public DateTime ModifiedDate { get; set; }

        [JsonIgnore, NotNull]
        public bool IsPending { get; set; }

        [JsonIgnore, Ignore]
        public bool IsNew { get; set; }

        public override string ToString()
        {
            return Pk;
        }

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            AbstractEntity abstractEntity = obj as AbstractEntity;

            if (abstractEntity == null)
            {
                return false;
            }

            // Return true if the fields match:
            return Pk == abstractEntity.Pk;
        }

        public override int GetHashCode()
        {
            return Pk.GetHashCode();
        }
    }

    /// <summary>
    /// Special JsonConvert resolver that allows you to ignore properties.  See http://stackoverflow.com/a/13588192/1037948
    /// </summary>
    public class IgnorableSerializerContractResolver : DefaultContractResolver
    {
        protected readonly Dictionary<Type, HashSet<string>> Ignores;

        public IgnorableSerializerContractResolver()
        {
            this.Ignores = new Dictionary<Type, HashSet<string>>();
        }

        /// <summary>
        /// Explicitly ignore the given property(s) for the given type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName">one or more properties to ignore.  Leave empty to ignore the type entirely.</param>
        public void Ignore(Type type, params string[] propertyName)
        {
            // start bucket if DNE
            if (!this.Ignores.ContainsKey(type)) this.Ignores[type] = new HashSet<string>();

            foreach (var prop in propertyName)
            {
                this.Ignores[type].Add(prop);
            }
        }

        /// <summary>
        /// Is the given property for the given type ignored?
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public bool IsIgnored(Type type, string propertyName)
        {
            if (!this.Ignores.ContainsKey(type)) return false;

            // if no properties provided, ignore the type entirely
            if (this.Ignores[type].Count == 0) return true;

            return this.Ignores[type].Contains(propertyName);
        }

        /// <summary>
        /// The decision logic goes here
        /// </summary>
        /// <param name="member"></param>
        /// <param name="memberSerialization"></param>
        /// <returns></returns>
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (this.IsIgnored(property.DeclaringType, property.PropertyName)
            // need to check basetype as well for EF -- @per comment by user576838
            /*|| this.IsIgnored(property.DeclaringType.BaseType, property.PropertyName)*/)
            {
                property.ShouldSerialize = instance => { return false; };
            }

            return property;
        }
    }

}
