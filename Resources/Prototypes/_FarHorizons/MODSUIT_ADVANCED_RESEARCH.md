# MODsuit Advanced Research - Deep Dive into SS14 Systems

## 🎨 **ToggleableVisuals System** - How Sealed States Work

### The Magic Behind Sealed/Unsealed Sprites

SS14 uses the **ToggleableVisuals** system to dynamically switch between sprite states. This is THE KEY to making your MODsuit show different sprites when deployed/sealed!

#### Component Structure:
```yaml
- type: ToggleableVisuals
  spriteLayer: "your_layer_name"
  clothingVisuals:
    head:  # Slot name
      - state: on-equipped-HELMET
    head-resomi:  # Species-specific variant
      - state: on-equipped-HELMET-resomi
```

#### How It Works:
1. **Appearance System**: Sets `ToggleableVisuals.Enabled = true/false`
2. **ToggleableVisualsSystem**: Listens for appearance changes
3. **Updates 3 Things**:
   - Entity sprite layer visibility
   - In-hand visuals
   - Clothing (equipped) visuals

#### Key Discovery from Code:
```csharp
// From ToggleableVisualsSystem.cs
protected override void OnAppearanceChange(EntityUid uid, ...)
{
    if (!AppearanceSystem.TryGetData<bool>(uid, ToggleableVisuals.Enabled, out var enabled))
        return;
    
    // Toggle sprite layer
    SpriteSystem.LayerSetVisible((uid, args.Sprite), layer, enabled);
    
    // Update item visuals (triggers clothing update)
    _item.VisualsChanged(uid);
}
```

### **For MODsuits**: Implementing Sealed States

You need to add `ToggleableVisuals` component to each part and link it to the sealed state:

```yaml
# Example: Helmet with sealed state
- type: entity
  id: ClothingHeadHelmetModsuitBasic
  components:
  - type: Sprite
    sprite: _FarHorizons/Clothing/Head/Modsuits/basic.rsi
    state: helmet  # Default unsealed icon
    layers:
    - state: helmet
      map: ["helmet"]
    - state: helmet-sealed
      map: ["helmet-sealed"]
      visible: false  # Hidden by default
  
  - type: Clothing
    sprite: _FarHorizons/Clothing/Head/Modsuits/basic.rsi
    # Base clothing visuals (unsealed)
  
  - type: ToggleableVisuals
    spriteLayer: "helmet-sealed"  # Which layer to toggle
    clothingVisuals:
      head:
        - state: equipped-HEAD-sealed
      head-resomi:
        - state: equipped-HEAD-sealed-resomi
  
  - type: Appearance  # Required for ToggleableVisuals to work
```

### Setting the Sealed State from Code:
```csharp
// In your SharedModsuitSystem.cs
private void SetPartSealed(EntityUid partUid, bool sealed)
{
    if (!TryComp<AppearanceComponent>(partUid, out var appearance))
        return;
    
    // This triggers ToggleableVisualsSystem to update sprites!
    _appearance.SetData(partUid, ToggleableVisuals.Enabled, sealed, appearance);
}
```

## 🔋 **Power Management System**

### PowerCell Integration Deep Dive

#### How Power Cells Work:
```yaml
- type: PowerCellSlot
  cellSlotId: cell_slot  # Unique identifier
- type: ItemSlots
  slots:
    cell_slot:
      name: power-cell-slot-component-slot-name-default
      startingItem: PowerCellHigh  # Pre-filled with high capacity cell
      whitelist:
        tags:
        - PowerCell
```

#### Power Draw Component (For Active MODsuits):
```csharp
// From PowerCellDrawComponent.cs
[DataField]
public float DrawRate = 1f;  // Watts drawn per second

[DataField]
public float UseRate;  // Joules consumed on activation

[DataField]
public TimeSpan Delay = TimeSpan.FromSeconds(1);  // How often to drain
```

#### Implementation for MODsuits:
```yaml
- type: entity
  id: ClothingBackpackModsuitBase
  components:
  - type: PowerCellSlot
    cellSlotId: cell_slot
  - type: ItemSlots
    slots:
      cell_slot:
        name: power-cell-slot-component-slot-name-default
        startingItem: PowerCellHigh
  - type: PowerCellDraw  # Add this for continuous power drain
    drawRate: 5.0  # 5W when idle
    useRate: 100   # 100J to activate
```

#### Checking Power in Code:
```csharp
// In SharedModsuitSystem.cs
private bool HasPower(EntityUid uid, ModsuitControlComponent component)
{
    if (!TryComp<PowerCellSlotComponent>(uid, out var cellSlot))
        return false;
    
    if (!_itemSlots.TryGetSlot(uid, cellSlot.CellSlotId, out var slot))
        return false;
    
    if (slot.Item == null)
        return false;
    
    if (!TryComp<PowerCellComponent>(slot.Item.Value, out var cell))
        return false;
    
    return cell.CurrentCharge > 0;
}

private bool TryUsePower(EntityUid uid, float amount)
{
    return _powerCell.TryUseCharge(uid, amount);
}
```

## 📦 **ItemMapper System** - Visual State Based on Contents

### What is ItemMapper?
Shows different sprite layers based on what items are inside containers. Perfect for showing which MODsuit modules are installed!

#### Example Usage:
```yaml
- type: ItemMapper
  mapLayers:
    module_jetpack:
      whitelist:
        tags:
        - ModsuitModuleJetpack
    module_storage:
      whitelist:
        tags:
        - ModsuitModuleStorage
    module_medical:
      whitelist:
        tags:
        - ModsuitModuleMedical
  sprite: _FarHorizons/Clothing/Back/Modsuits/basic.rsi
```

#### How It Works:
1. Scans containers for items matching whitelists
2. Shows/hides sprite layers based on what's found
3. Can use minCount/maxCount for quantity-based visuals

### For MODsuits:
```yaml
- type: entity
  id: ClothingBackpackModsuitBasic
  components:
  - type: Sprite
    sprite: _FarHorizons/Clothing/Back/Modsuits/basic.rsi
    layers:
    - state: control
      map: ["base"]
    - state: module-jetpack
      map: ["module_jetpack"]
      visible: false
    - state: module-storage
      map: ["module_storage"]
      visible: false
  
  - type: ItemMapper
    mapLayers:
      module_jetpack:
        whitelist:
          components:
          - ModsuitModuleJetpack
      module_storage:
        whitelist:
          components:
          - ModsuitModuleStorage
    sprite: _FarHorizons/Clothing/Back/Modsuits/basic.rsi
    spriteLayers:
    - module_jetpack
    - module_storage
    containerWhitelist:
    - modsuit-modules  # Only check the modules container
```

## 🎭 **Species-Specific Sprites**

### How Species Variants Work:

SS14 supports automatic species-specific sprite rendering through the inventory system:

```yaml
clothingVisuals:
  head:  # Default human
    - state: equipped-HEAD
  head-vox:  # Vox species
    - state: equipped-HEAD-vox
  head-vulpkanin:  # Vulpkanin species
    - state: equipped-HEAD-vulpkanin
  head-resomi:  # Resomi species
    - state: equipped-HEAD-resomi
  head-avali:  # Avali species
    - state: equipped-HEAD-avali
```

### The System Logic:
```csharp
// From ToggleableVisualsSystem.cs
private void OnGetEquipmentVisuals(...)
{
    List<PrototypeLayerData>? layers = null;
    
    // Try species-specific first
    if (inventory.SpeciesId != null)
        component.ClothingVisuals.TryGetValue($"{args.Slot}-{inventory.SpeciesId}", out layers);
    
    // Fall back to default if no species variant
    if (layers == null)
        component.ClothingVisuals.TryGetValue(args.Slot, out layers);
}
```

### For MODsuits - Full Species Support:
```yaml
- type: Clothing
  sprite: _FarHorizons/Clothing/Back/Modsuits/basic.rsi
  # Unsealed states
  
- type: ToggleableVisuals
  spriteLayer: "sealed"
  clothingVisuals:
    # Sealed states with species support
    backpack:
      - state: equipped-BACKPACK-sealed
    backpack-resomi:
      - state: equipped-BACKPACK-sealed-resomi
    backpack-vox:
      - state: equipped-BACKPACK-sealed-vox
```

## 🔧 **ModsuitPart Component Enhancement**

### Adding Sealed State Management:

Update your `ModsuitPartComponent.cs`:

```csharp
[DataField, AutoNetworkedField]
public bool Sealed;

[DataField]
public string SealedLayer = "sealed";  // Which sprite layer is the sealed overlay
```

### System Integration:

```csharp
// In SharedModsuitSystem.cs
private void SetPartSealed(EntityUid partUid, bool sealed)
{
    if (!TryComp<ModsuitPartComponent>(partUid, out var partComp))
        return;
    
    partComp.Sealed = sealed;
    Dirty(partUid, partComp);
    
    // Update appearance for ToggleableVisuals
    if (TryComp<AppearanceComponent>(partUid, out var appearance))
        _appearance.SetData(partUid, ToggleableVisuals.Enabled, sealed, appearance);
}

private void DeployPart(EntityUid uid, ModsuitControlComponent component, EntityUid? partUid, string slot, EntityUid user)
{
    if (partUid == null || component.Wearer == null)
        return;
    
    if (_inventory.TryEquip(component.Wearer.Value, partUid.Value, slot, force: true))
    {
        // SEAL THE PART when deployed
        SetPartSealed(partUid.Value, true);
    }
}

private void RetractPart(EntityUid uid, ModsuitControlComponent component, EntityUid? partUid, string containerId)
{
    if (partUid == null)
        return;
    
    // UNSEAL THE PART when retracted
    SetPartSealed(partUid.Value, false);
    
    if (_inventory.TryUnequip(component.Wearer!.Value, partUid.Value, force: true))
    {
        var container = _container.EnsureContainer<Container>(uid, containerId);
        _container.Insert(partUid.Value, container);
    }
}
```

## 🎬 **Animation & Visual Effects**

### Animated Sprites (Delays):

```json
{
  "name": "control-sealed",
  "delays": [
    [0.1, 0.1, 0.1, 0.1]  // 4 frames, 0.1s each
  ]
}
```

### Glowing Effects on Activation:

```yaml
- type: PointLight
  enabled: false  # Start disabled
  radius: 2
  energy: 1.5
  color: "#00FFFF"  # Cyan glow

- type: ItemTogglePointLight
  toggleableVisualsColorModulatesLights: true  # Use ToggleableVisuals color
```

### Sound Effects:

```yaml
- type: ModsuitControl
  deploySound:
    path: /Audio/Mecha/mechmove03.ogg
    params:
      volume: -5
  activateSound:
    path: /Audio/Effects/powerup.ogg
  deactivateSound:
    path: /Audio/Effects/powerdown.ogg
```

## 🧩 **Module System Advanced**

### Module Complexity Tracking:

```csharp
// In SharedModsuitSystem.cs
private bool CanInstallModule(EntityUid uid, ModsuitControlComponent control, EntityUid module)
{
    if (!TryComp<ModsuitModuleComponent>(module, out var moduleComp))
        return false;
    
    if (control.ComplexityUsed + moduleComp.Complexity > control.ComplexityMax)
    {
        _popup.PopupClient(Loc.GetString("modsuit-complexity-exceeded"), uid, control.Wearer);
        return false;
    }
    
    return true;
}

private void InstallModule(EntityUid uid, ModsuitControlComponent control, EntityUid module)
{
    if (!TryComp<ModsuitModuleComponent>(module, out var moduleComp))
        return;
    
    control.ComplexityUsed += moduleComp.Complexity;
    control.Modules.Add(module);
    
    var container = _container.EnsureContainer<Container>(uid, control.ModulesContainerId);
    _container.Insert(module, container);
    
    Dirty(uid, control);
}
```

### Module Activation:

```csharp
public void ToggleModule(EntityUid uid, ModsuitControlComponent control, EntityUid module)
{
    if (!TryComp<ModsuitModuleComponent>(module, out var moduleComp))
        return;
    
    if (!control.Active)
    {
        _popup.PopupClient(Loc.GetString("modsuit-not-active"), uid, control.Wearer);
        return;
    }
    
    moduleComp.Active = !moduleComp.Active;
    
    if (moduleComp.Active)
    {
        control.IdlePowerDrain += moduleComp.ActivePowerUse;
        RaiseLocalEvent(module, new ModsuitModuleActivatedEvent(uid));
    }
    else
    {
        control.IdlePowerDrain -= moduleComp.ActivePowerUse;
        RaiseLocalEvent(module, new ModsuitModuleDeactivatedEvent(uid));
    }
    
    Dirty(uid, control);
    Dirty(module, moduleComp);
}
```

## 📊 **Performance Optimization**

### Container Management Best Practices:

```csharp
// Cache containers instead of repeated lookups
private Container GetOrCreatePartContainer(EntityUid uid, string containerId)
{
    return _container.EnsureContainer<Container>(uid, containerId);
}

// Batch equipment operations
private void DeployAllParts(EntityUid uid, ModsuitControlComponent component, EntityUid user)
{
    var parts = new[]
    {
        (component.Helmet, "head"),
        (component.Chestplate, "outerClothing"),
        (component.Gauntlets, "gloves"),
        (component.Boots, "shoes")
    };
    
    foreach (var (partUid, slot) in parts)
    {
        if (partUid.HasValue)
            DeployPart(uid, component, partUid, slot, user);
    }
}
```

## 🎯 **Complete Implementation Checklist**

### 1. Components Needed:
- ✅ `ModsuitControlComponent` (control unit)
- ✅ `ModsuitPartComponent` (helmet, chestplate, gauntlets, boots)
- ✅ `ModsuitModuleComponent` (modules)
- ✅ `ToggleableVisuals` (sealed states)
- ✅ `AppearanceComponent` (visual updates)
- ✅ `HideLayerClothing` (body coverage)
- ✅ `PowerCellSlot` (power management)
- ⚠️ `ItemMapper` (optional - module visualization)

### 2. Sprite States Required:
```
icon.png                              (inventory icon)
[part].png                            (unsealed world sprite)
[part]-sealed.png                     (sealed world sprite)
equipped-[SLOT].png (4 dirs)          (unsealed equipped)
equipped-[SLOT]-sealed.png (4 dirs)   (sealed equipped)
equipped-[SLOT]-[species].png         (species variants)
```

### 3. System Hooks:
- `OnControlStartup` - Initialize actions
- `OnControlEquipped` - Track wearer
- `OnDeployAction` - Deploy all parts
- `OnActivateAction` - Toggle power
- `DeployPart` - Equip + seal individual part
- `RetractPart` - Unseal + unequip + store
- `SetActive` - Power on/off with effects

### 4. Power Management Integration:
```csharp
public override void Update(float frameTime)
{
    base.Update(frameTime);
    
    foreach (var (uid, component) in EntityQuery<ModsuitControlComponent>())
    {
        if (!component.Active || component.Wearer == null)
            continue;
        
        // Drain power based on idle + modules
        var powerDrain = component.IdlePowerDrain * frameTime;
        
        if (!TryUsePower(uid, powerDrain))
        {
            // Out of power!
            SetActive(uid, component, false);
            _popup.PopupClient(Loc.GetString("modsuit-power-depleted"), uid, component.Wearer.Value);
        }
    }
}
```

## 🚀 **Advanced Features to Implement**

### 1. Deployment Animations:
- DoAfter system for gradual deployment
- Step-by-step part equipping with delays
- Visual/audio feedback per part

### 2. Damage States:
- Damaged appearance based on armor integrity
- Warning overlays when critical
- Malfunction effects when over-damaged

### 3. Module UI:
- Interactive panel for module management
- Visual complexity bar
- Module toggle buttons

### 4. Environmental Reactions:
- Glow effects in dark areas
- Steam effects in hot areas
- Frost effects in cold areas

### 5. Power Efficiency:
- Lower drain when stationary
- Higher drain when sprinting
- Module-specific power scaling

## 📚 **Key Files for Reference**

### Core Systems:
- `Content.Shared/Clothing/EntitySystems/ClothingSystem.cs`
- `Content.Shared/Clothing/EntitySystems/ToggleableClothingSystem.cs`
- `Content.Shared/Clothing/EntitySystems/HideLayerClothingSystem.cs`
- `Content.Client/Toggleable/ToggleableVisualsSystem.cs`

### Power Management:
- `Content.Server/PowerCell/PowerCellSystem.cs`
- `Content.Shared/PowerCell/PowerCellDrawComponent.cs`

### Visuals:
- `Content.Shared/Storage/Components/ItemMapperComponent.cs`
- `Content.Shared/Appearance/SharedAppearanceSystem.cs`

### Examples:
- `Resources/Prototypes/Entities/Clothing/Head/base_clothinghead.yml` (line 149: ClothingHeadHardsuitBase)
- `Resources/Prototypes/Entities/Clothing/OuterClothing/hardsuits.yml`
- `Resources/Prototypes/_StarLight/Entities/Clothing/Head/hardsuit-helmets.yml` (ToggleableVisuals examples)

---

## 🎓 **Pro Implementation Tips**

1. **Always use Appearance + ToggleableVisuals together** for state changes
2. **Cache container references** instead of repeated lookups
3. **Use DoAfter** for deployment to prevent instant equipment
4. **Add sound feedback** at every state transition
5. **Test with multiple species** to verify sprite rendering
6. **Implement power checks** before every major operation
7. **Use ItemMapper** to show installed modules visually
8. **Add examination text** to show power level and active modules
9. **Consider network bandwidth** - batch state updates
10. **Implement proper cleanup** in component shutdown events

---

**This system combines:**
- ToggleableClothing (helmet attachment) 
- ToggleableVisuals (sealed states)
- HideLayerClothing (body coverage)
- PowerCell (power management)
- ItemMapper (module visualization)
- Container (part/module storage)
- Inventory (equipment management)

**Into a complete, production-ready MODsuit system!**
