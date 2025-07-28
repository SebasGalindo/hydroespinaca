using HydroEspinaca.Shared.Abstractions;

namespace HydroEspinaca.Shared.Mongo.Interfaces;

public interface IEntityMapper<TEntity, TDocument> where TEntity : IIdentifiableMutable where TDocument : IIdentifiableMutable
{
    TEntity ToEntity(TDocument doc);
    TDocument ToDocument(TEntity entity);
}
