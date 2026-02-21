using System;
using System.Numerics;
using Anyder.Objects.Vfx;
using Dalamud.Game.ClientState.Objects.Types;

namespace Anyder.Objects;

public enum ObjectType
{
    Model,
    SharedGroup,
    StaticVfx,
    ActorVfx,
    Invalid
}

public class SpawnedObject : IDisposable
{
    public ObjectType Type { get; private set; }
    public string Path { get; private set; }
    public string Name { get; set; }

    public Model? Model { get; private set; }
    public Group? Group { get; private set; }
    public BaseVfx? Vfx { get; private set; }
    
    public bool IsValid => Type != ObjectType.Invalid;

    public SpawnedObject(string path, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null, bool collide = false)
    {
        Path = path;
        Name = Path;
        
        string ext = System.IO.Path.GetExtension(path);
        
        var pos = position ?? Vector3.Zero;
        var rot = rotation ?? Quaternion.Identity;
        var sca = scale ?? Vector3.One;
        
        switch (ext)
        {
            case ".avfx":
                Type = ObjectType.StaticVfx;
                Vfx = new StaticVfx(path, pos, sca, 0f, loop: true);
                break;
            case ".mdl":
                Type = ObjectType.Model;
                Model = new Model(path, pos, rot, sca);
                break;
            case ".sgb":
                Type = ObjectType.SharedGroup;
                Group = new Group(path, pos, rot, sca, collide);
                break;
            default:
                Type = ObjectType.Invalid;
                AnyderService.Log.Error($"Unsupported extension {ext}");
                break;
        }
    }

    public SpawnedObject(string path, IGameObject target, bool collide = false, int seconds = 5, bool loop = false)
    {
        Path = path;
        Name = Path;
        
        string ext = System.IO.Path.GetExtension(path);

        var pos = target.Position;
        var rot = Quaternion.CreateFromAxisAngle(Vector3.UnitX, target.Rotation);
        var sca = Vector3.One;
        
        switch (ext)
        {
            case ".avfx":
                Type = ObjectType.ActorVfx;
                Vfx = new ActorVfx(path, target, target, TimeSpan.FromSeconds(seconds), loop);
                break;
            case ".mdl":
                Type = ObjectType.Model;
                Model = new Model(path, pos, rot, sca);
                break;
            case ".sgb":
                Type = ObjectType.SharedGroup;
                Group = new Group(path, pos, rot, sca, collide);
                break;
            default:
                Type = ObjectType.Invalid;
                AnyderService.Log.Error($"Unsupported extension {ext}");
                break;
        }
    }
    
    // Overloaded constructors for pre-instantiated objects
    public SpawnedObject(Model model)
    {
        Type = ObjectType.Model;
        Model = model;
        Path = model.Path;
        Name = Path;
    }

    public SpawnedObject(Group group)
    {
        Type = ObjectType.SharedGroup;
        Group = group;
        Path = group.Path;
        Name = Path;
    }

    public SpawnedObject(BaseVfx vfx)
    {
        Type = vfx is StaticVfx ? ObjectType.StaticVfx : ObjectType.ActorVfx;
        Vfx = vfx;
        Path = vfx.Path;
        Name = Path;
    }
    
    /// <summary>
    /// Gets the underlying Transform object if the spawned object supports it.
    /// </summary>
    /// <returns>The Transform object, or null if unsupported (like VFX).</returns>
    public Transform? GetTransform()
    {
        return Type switch
        {
            ObjectType.Model => Model?.Transform,
            ObjectType.SharedGroup => Group?.Transform,
            ObjectType.StaticVfx => ((StaticVfx?)Vfx)?.Transform,
            _ => null
        };
    }

    /// <summary>
    /// Updates the position, rotation, and/or scale of the object.
    /// Only updates the values that are provided.
    /// </summary>
    public void SetTransform(Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null)
    {
        var transform = GetTransform();
        
        if (transform != null)
        {
            if (position.HasValue) transform.Position = position.Value;
            if (rotation.HasValue) transform.Rotation = rotation.Value;
            if (scale.HasValue) transform.Scale = scale.Value;
            transform.Update();
        }
        else if (Type == ObjectType.ActorVfx)
        {
            AnyderService.Log.Warning($"Cannot set transform for ActorVfx ({Path}).");
        }
    }

    public void Dispose()
    {
        Model?.Dispose();
        Group?.Dispose();
        Vfx?.Dispose();
    }
}
