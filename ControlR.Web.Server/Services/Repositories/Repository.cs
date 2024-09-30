﻿using Microsoft.EntityFrameworkCore;

namespace ControlR.Web.Server.Services.Repositories;

public interface IRepository
{
  Task<int> Count<TEntity>()
    where TEntity : class;
  Task<TEntity> AddOrUpdate<TEntity, TDto>(TDto dto, Func<TEntity> createFactory)
    where TEntity : EntityBase
    where TDto : EntityBaseDto;
  IQueryable<TEntity> AsQueryable<TEntity>() 
    where TEntity : class;
  Task<bool> Delete<TEntity>(int id) 
    where TEntity : class;
  Task<TEntity?> FirstOrDefault<TEntity>(
    Func<IQueryable<TEntity>, Task<TEntity?>> filter,
    Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeBuilder = null)
    where TEntity : class;
  Task<List<TEntity>> GetAll<TEntity>(Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeBuilder = null)
    where TEntity : class;
  Task<List<TProjection>> GetAll<TEntity, TProjection>(
    Func<TEntity, TProjection> selectProjection,
    Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeBuilder = null)
    where TEntity : class;
  
  Task<TEntity?> GetById<TEntity>(int id, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeBuilder = null) 
    where TEntity : EntityBase;
  Task<TEntity?> GetByUid<TEntity>(Guid uid, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeBuilder = null) 
    where TEntity : EntityBase;
  Task<List<TEntity>> GetWhere<TEntity>(
    Func<IQueryable<TEntity>, IQueryable<TEntity>> whereFilter, 
    Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeBuilder = null) 
    where TEntity : class;

  Task<List<TProjection>> GetWhere<TEntity, TProjection>(
    Func<IQueryable<TEntity>, IQueryable<TEntity>> whereFilter,
    Func<TEntity, TProjection> selectProjection,
    Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeBuilder = null)
    where TEntity : class;

  Task<TEntity?> UpdatePartial<TEntity>(object partialDto, Guid uid) 
    where TEntity : EntityBase;
  Task<TEntity?> UpdatePartial<TEntity>(object partialDto, int id) 
    where TEntity : EntityBase;
}

public class Repository(AppDb _appDb) : IRepository
{
  public async Task<TEntity> AddOrUpdate<TEntity, TDto>(
    TDto dto,
    Func<TEntity> createFactory)
    where TEntity : EntityBase
    where TDto : EntityBaseDto
  {
    var set = _appDb.Set<TEntity>();
    var entity = await set.FirstOrDefaultAsync(x => x.Uid == dto.Uid);

    if (entity is not null)
    {
      set
        .Update(entity)
        .CurrentValues
        .SetValues(dto);
    }
    else
    {
      entity = createFactory.Invoke();
      set
        .Add(entity)
        .CurrentValues
        .SetValues(dto);

    }

    await _appDb.SaveChangesAsync();
    return entity;
  }

  public IQueryable<TEntity> AsQueryable<TEntity>()
     where TEntity : class
  {
    return _appDb.Set<TEntity>().AsQueryable();
  }

  public async Task<int> Count<TEntity>()
    where TEntity : class
  {
    return await _appDb.Set<TEntity>().CountAsync();
  }

  public async Task<bool> Delete<TEntity>(int id)
    where TEntity : class
  {
    var set = _appDb.Set<TEntity>();
    var entity = await set.FindAsync(id);
    if (entity is null)
    {
      return false;
    }

    set.Remove(entity);
    await _appDb.SaveChangesAsync();
    return true;
  }

  public async Task<TEntity?> FirstOrDefault<TEntity>(
    Func<IQueryable<TEntity>, Task<TEntity?>> filter,
    Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeBuilder = null) where TEntity : class
  {
    var query = _appDb.Set<TEntity>().AsQueryable();
    if (includeBuilder is not null)
    {
      query = includeBuilder(query);
    }

    return await filter(query);
  }

  public async Task<List<TEntity>> GetAll<TEntity>(Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeBuilder = null)
    where TEntity : class
  {
    var query = _appDb.Set<TEntity>().AsQueryable();
    if (includeBuilder is not null)
    {
      query = includeBuilder(query);
    }

    return await query.ToListAsync();
  }
  public async Task<List<TProjection>> GetAll<TEntity, TProjection>(
    Func<TEntity, TProjection> selectProjection,
    Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeBuilder = null)
    where TEntity : class
  {
    
    var query = _appDb.Set<TEntity>().AsQueryable().Select(x => x);
    if (includeBuilder is not null)
    {
      query = includeBuilder(query);
    }
    
    var selected = query.Select(x => selectProjection(x));

    return await selected.ToListAsync();
  }

  public async Task<TEntity?> GetById<TEntity>(int id, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeBuilder = null)
    where TEntity : EntityBase
  {
    var query = _appDb.Set<TEntity>().AsQueryable();
    if (includeBuilder is not null)
    {
      query = includeBuilder.Invoke(query);
    }

    var entity = await query.FirstOrDefaultAsync(x => x.Id == id);
    return entity;
  }

  public async Task<TEntity?> GetByUid<TEntity>(
    Guid uid,
    Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeBuilder = null)
    where TEntity : EntityBase
  {
    var query = _appDb.Set<TEntity>().AsQueryable();
    if (includeBuilder is not null)
    {
      query = includeBuilder.Invoke(query);
    }

    var entity = await query.FirstOrDefaultAsync(x => x.Uid == uid);
    return entity;
  }

  public async Task<List<TEntity>> GetWhere<TEntity>(
    Func<IQueryable<TEntity>, IQueryable<TEntity>> whereFilter,
    Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeBuilder = null)
    where TEntity : class
  {
    var query = _appDb.Set<TEntity>().AsQueryable();
    if (includeBuilder is not null)
    {
      query = includeBuilder(query);
    }

    query = whereFilter(query);
    var entities = await query.ToListAsync();
    return entities;
  }

  public async Task<List<TProjection>> GetWhere<TEntity, TProjection>(
    Func<IQueryable<TEntity>, IQueryable<TEntity>> whereFilter, 
    Func<TEntity, TProjection> selectProjection, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeBuilder = null) 
    where TEntity : class
  {
    var query = _appDb.Set<TEntity>().AsQueryable();
    if (includeBuilder is not null)
    {
      query = includeBuilder(query);
    }

    query = whereFilter(query);
    var selected = query.Select(x => selectProjection(x));
    var entities = await selected.ToListAsync();
    return entities;
  }

  public async Task<TEntity?> UpdatePartial<TEntity>(object partialDto, int id)
    where TEntity : EntityBase
  {
    var set = _appDb.Set<TEntity>();
    var entity = await set.FindAsync(id);

    if (entity is null)
    {
      return null;
    }

    _appDb.Entry(entity).CurrentValues.SetValues(partialDto);
    await _appDb.SaveChangesAsync();
    return entity;
  }

  public async Task<TEntity?> UpdatePartial<TEntity>(object partialDto, Guid uid)
    where TEntity : EntityBase
  {
    var set = _appDb.Set<TEntity>();
    var entity = await set.FirstOrDefaultAsync(x => x.Uid == uid);

    if (entity is null)
    {
      return null;
    }

    _appDb.Entry(entity).CurrentValues.SetValues(partialDto);
    await _appDb.SaveChangesAsync();
    return entity;
  }
}