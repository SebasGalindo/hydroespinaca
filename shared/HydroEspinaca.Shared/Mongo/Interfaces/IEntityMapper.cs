namespace HydroEspinaca.Shared.Mongo.Interfaces;

public interface IEntityMapper<TEntity, TDocument>
{
    TEntity ToEntity(TDocument doc);
    TDocument ToDocument(TEntity entity);
}
