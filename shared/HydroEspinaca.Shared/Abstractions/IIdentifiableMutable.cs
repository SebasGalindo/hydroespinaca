namespace HydroEspinaca.Shared.Abstractions;
public interface IIdentifiableMutable : IIdentifiable
{
    void SetId(string id);
}