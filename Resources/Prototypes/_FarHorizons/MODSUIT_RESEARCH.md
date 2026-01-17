# MODsuit Research - How Hardsuits Work in SS14

## 🔧 How Hardsuit Toggle/Closing Works

### The ToggleableClothing System
Hardsuits use the **ToggleableClothing** component to deploy/retract helmets:

```yaml
- type: ToggleableClothing
  clothingPrototype: ClothingHeadHelmetHardsuitAtmos  # What helmet to spawn
  slot: head  # Where to equip it
```

**How it works:**
1. Helmet is stored inside the suit's container
2. When you toggle, it equips the helmet from internal storage
3. When you toggle again, it unequips and stores it back

### For MODsuits - Multi-Part Deployment
Our MODsuit system extends this to deploy ALL parts at once:
- Control unit on back contains: helmet, chestplate, gauntlets, boots
- Deploy action equips all 4 parts from internal containers
- Retract action unequips and stores all 4 parts back

## 🎨 How to Hide Body Parts (Cover Exposed Skin)

### HideLayerClothing Component
This is THE KEY to making suits cover the body properly!

```yaml
- type: HideLayerClothing
  slots:
  - Hair        # Hides hair
  - Snout       # Hides snouts (for species)
  - HeadTop     # Hides head accessories
  - HeadSide    # Hides side head accessories
  - FacialHair  # Hides beards/facial hair
```

**Available Layers to Hide:**
- `Hair` - Hair on head
- `FacialHair` - Beards, mustaches
- `Snout` - Species snouts
- `HeadTop` - Top of head accessories
- `HeadSide` - Side of head accessories
- `Eyes` - Eye accessories
- `Chest` - Chest (for hiding uniform under suits)
- `RArm` / `LArm` - Arms
- `RHand` / `LHand` - Hands
- `RLeg` / `LLeg` - Legs
- `RFoot` / `LFoot` - Feet
- `Tail` - Tails
- `Wings` - Wings

### How to Apply to MODsuits

**For Helmets** (already added to your modsuit-helmets.yml):
```yaml
- type: HideLayerClothing
  slots:
  - Hair
  - Snout
  - HeadTop
  - HeadSide
```

**For Chestplates** (add this to hide body parts):
```yaml
- type: HideLayerClothing
  slots:
  - Chest      # Hides the chest/uniform underneath
  - RArm       # Hides right arm
  - LArm       # Hides left arm
```

**For Boots** (add this to hide feet):
```yaml
- type: HideLayerClothing
  slots:
  - RFoot
  - LFoot
```

## 🖼️ Action Icons

### Existing Action Icons Found:
Located in: `Resources/Textures/Interface/Actions/`

**Toggle Suit Action:**
- Uses: `Interface/VerbIcons/outfit.svg.192dpi.png`
- This is the icon for toggling suit pieces (helmet on/off)

**Power/Activation Icons:**
You can use these for MODsuit power on/off:
- `Interface/Actions/harm.png` / `harmOff.png` (red icons)
- `Interface/Actions/eyeopen.png` / `eyeclose.png` (eye icons)
- Or create custom icons in a new RSI

### Action Definition Pattern:
```yaml
- type: entity
  id: ActionToggleSuitPiece
  name: Toggle Suit Piece
  description: Remember to equip important pieces before action.
  components:
  - type: Action
    itemIconStyle: BigItem
    useDelay: 1
  - type: InstantAction
    event: !type:ToggleClothingEvent
```

## 📐 Sprite Coverage - How to Make Suits Cover Body

### The Secret: Layering System
Clothing sprites are drawn in layers on the mob. To cover exposed body:

1. **Make your sprites bigger** - Draw over where skin would show
2. **Use sealed state sprites** - The "-sealed" sprites should cover more area
3. **Layer order matters** - Outer clothing is drawn OVER the body

### Sprite Drawing Tips:

**For Chestplate (outerClothing):**
- Draw arms on your sprite to cover exposed arm skin
- Draw torso coverage that goes over the uniform
- When sealed, make it bulkier/more coverage

**For Helmet:**
- Full head coverage in sealed state
- Draw over where hair/face would be
- Use IdentityBlocker component to hide face

**For Gauntlets:**
- Draw forearm coverage, not just hands
- Extend up the arm to meet chestplate

**For Boots:**
- Draw shin/calf coverage
- Extend up the leg to meet pants/suit

### Example: Hardsuit vs MODsuit Coverage

**Hardsuit (current):**
- Helmet hides hair/face (HideLayerClothing)
- Suit draws over body with sprite coverage
- Helmet toggles from suit

**MODsuit (yours):**
- Helmet hides hair/face (HideLayerClothing) ✅ Added
- Chestplate should hide chest/arms (ADD THIS)
- All 4 parts deploy at once (already coded)
- Sealed state = fully covered, unsealed = more open

## 🎯 What You Need to Update

### 1. Add HideLayerClothing to Chestplate
In `Resources/Prototypes/_FarHorizons/Entities/Clothing/OuterClothing/modsuits.yml`:
```yaml
- type: HideLayerClothing
  slots:
  - Chest
  - RArm
  - LArm
```

### 2. Add HideLayerClothing to Boots
In `Resources/Prototypes/_FarHorizons/Entities/Clothing/Shoes/modsuit-boots.yml`:
```yaml
- type: HideLayerClothing
  slots:
  - RFoot
  - LFoot
```

### 3. When Drawing Sprites
- **Icon state**: Just the item (what you see in inventory)
- **Equipped state**: Draw the item ON the character body
- **Equipped-sealed state**: Draw MORE coverage, armor plates extended, fully covered

**Think of it like this:**
- Unsealed = casual mode, joints exposed, plates retracted
- Sealed = combat mode, fully armored, everything covered

### 4. Create Custom Action Icons (Optional)
Create your own RSI for modsuit actions:
- Deploy/retract icon (suit closing/opening)
- Power on/off icon (glowing/dark)
- Module panel icon (wrench/gear)

Put them in: `Resources/Textures/Interface/Actions/modsuit.rsi/`

## 🔍 Key Files Reference

**Toggle System:**
- `Content.Shared/Clothing/EntitySystems/ToggleableClothingSystem.cs`
- `Content.Shared/Clothing/Components/ToggleableClothingComponent.cs`

**Hide Layer System:**
- `Content.Shared/Clothing/Components/HideLayerClothingComponent.cs`
- `Content.Shared/Clothing/EntitySystems/HideLayerClothingSystem.cs`

**Action Icons:**
- `Resources/Prototypes/Actions/types.yml` (line 232: ActionToggleSuitPiece)
- `Resources/Textures/Interface/Actions/` (all action icons)

**Base Hardsuit Reference:**
- `Resources/Prototypes/Entities/Clothing/OuterClothing/base_clothingouter.yml` (line 103: ClothingOuterHardsuitBase)
- `Resources/Prototypes/Entities/Clothing/Head/base_clothinghead.yml` (line 149: ClothingHeadHardsuitBase)

## 💡 Pro Tips

1. **Species Variants**: Hardsuits support different sprites per species (vox, avali, etc.)
2. **Lights**: You can add headlamp overlays with HandheldLight component
3. **Sounds**: Use /Audio/Mecha/mechmove03.ogg for that authentic hardsuit equip sound
4. **Identity Blocking**: IdentityBlocker component on helmet hides player name
5. **Breath Mask**: BreathMask component lets you use internals while worn

## 🎨 Visual Example Structure

```
Normal Body:
  └─ Hair visible
  └─ Uniform visible  
  └─ Arms/legs visible

MODsuit Deployed (Sealed):
  └─ Helmet OVER hair (HideLayerClothing: Hair)
  └─ Chestplate OVER uniform (HideLayerClothing: Chest, Arms)
  └─ Gauntlets cover hands/forearms (sprites)
  └─ Boots cover feet/calves (HideLayerClothing: Feet)
  └─ Result: Full body coverage!
```

The combination of **HideLayerClothing** (hides body layers) + **good sprite coverage** (draws over remaining areas) = complete body coverage!
