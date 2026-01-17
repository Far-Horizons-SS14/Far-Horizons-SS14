# MODsuit System for Far Horizons

## Overview
This is a comprehensive modular suit (MODsuit) system based on TG Station's MODsuit mechanics. MODsuits are advanced powered exoskeletons that provide environmental protection and can be enhanced with modules.

## What is a MODsuit?
A MODsuit (Modular Outerwear Device) consists of:
- **Control Unit**: The main backpack item that contains the power cell and manages all systems
- **Helmet**: Head protection that seals for EVA
- **Chestplate**: Main torso armor
- **Gauntlets**: Hand protection
- **Boots**: Foot protection with magnetic grips

## How MODsuits Work

### Deployment
1. **Wear the control unit** on your back
2. **Use the Deploy action** to deploy all parts at once
3. Parts are automatically equipped from the control unit's internal storage
4. **Use the Deploy action again** to retract all parts back into the control unit

### Activation
1. **Deploy the suit first** (all parts must be equipped)
2. **Use the Activate action** to power on the suit
3. When active, the suit:
   - Reduces movement slowdown
   - Powers installed modules
   - Drains the power cell
   - Provides full environmental protection

### Power Management
- MODsuits require a power cell to function
- Power drains while the suit is active
- Installed modules may have additional power requirements
- Replace the power cell by accessing the control unit's power cell slot

### Modules
Modules add special abilities to your MODsuit:

#### Module Types
- **Passive**: Always active, no interaction needed
- **Toggle**: Can be turned on/off
- **Usable**: Single-use activation
- **Active**: Can be selected and used actively

#### Module Installation
1. Open the module hatch (right-click verb on control unit)
2. Insert modules into the control unit
3. Each module has a complexity value
4. Total complexity cannot exceed the suit's maximum (default: 15)

## Creating New MODsuits

### File Structure
When creating a new modsuit variant, you need sprites in these folders:
```
Resources/Textures/_FarHorizons/Clothing/
├── Back/Modsuits/yoursuit.rsi/
│   ├── meta.json
│   ├── icon.png
│   ├── equipped-BACKPACK.png (4 directions)
│   └── equipped-BACKPACK-sealed.png (4 directions)
├── Head/Modsuits/yoursuit.rsi/
│   ├── meta.json
│   ├── icon.png
│   ├── equipped-HELMET.png (4 directions)
│   └── equipped-HELMET-sealed.png (4 directions)
├── OuterClothing/Modsuits/yoursuit.rsi/
│   ├── meta.json
│   ├── icon.png
│   ├── equipped-OUTERCLOTHING.png (4 directions)
│   └── equipped-OUTERCLOTHING-sealed.png (4 directions)
├── Hands/Modsuits/yoursuit.rsi/
│   ├── meta.json
│   ├── icon.png
│   ├── equipped-HAND.png (4 directions)
│   └── equipped-HAND-sealed.png (4 directions)
└── Shoes/Modsuits/yoursuit.rsi/
    ├── meta.json
    ├── icon.png
    ├── equipped-FEET.png (4 directions)
    └── equipped-FEET-sealed.png (4 directions)
```

### Prototype Example
Create your modsuit in `Resources/Prototypes/_FarHorizons/Entities/Clothing/Back/modsuits.yml`:

```yaml
- type: entity
  id: ClothingBackpackModsuitYourSuit
  parent: ClothingBackpackModsuitBase
  name: your MODsuit
  description: Your custom modsuit description.
  components:
  - type: Sprite
    sprite: _FarHorizons/Clothing/Back/Modsuits/yoursuit.rsi
  - type: Clothing
    sprite: _FarHorizons/Clothing/Back/Modsuits/yoursuit.rsi
  - type: ModsuitControl
    helmetPrototype: ClothingHeadHelmetModsuitYourSuit
    chestplatePrototype: ClothingOuterModsuitYourSuit
    gauntletsPrototype: ClothingHandsGlovesModsuitYourSuit
    bootsPrototype: ClothingShoesBootsModsuitYourSuit
    complexityMax: 20  # How many modules can be installed
    slowdownInactive: 1.5  # Movement penalty when not active
    slowdownActive: 0.5    # Movement penalty when active
```

Then create corresponding part prototypes in:
- `Resources/Prototypes/_FarHorizons/Entities/Clothing/Head/modsuit-helmets.yml`
- `Resources/Prototypes/_FarHorizons/Entities/Clothing/OuterClothing/modsuits.yml`
- `Resources/Prototypes/_FarHorizons/Entities/Clothing/Hands/modsuit-gauntlets.yml`
- `Resources/Prototypes/_FarHorizons/Entities/Clothing/Shoes/modsuit-boots.yml`

## Basic MODsuit (Prototype)
The basic MODsuit is included as a reference implementation and parent for other modsuits:
- **ID**: `ClothingBackpackModsuitBasic`
- **Filled ID**: `ClothingBackpackModsuitBasicFilled` (includes power cell)
- Lightweight civilian suit
- Complexity limit: 15
- Minimal armor
- Good for general EVA and maintenance work

## Customization Options

### Control Unit Properties
- `complexityMax`: Maximum module complexity
- `slowdownInactive`: Movement penalty when suit is off
- `slowdownActive`: Movement penalty when suit is on
- `activationStepTime`: Time to deploy each part
- `idlePowerDrain`: Power drain per second when active

### Part Properties
Each part (helmet, chestplate, gauntlets, boots) can have:
- Custom armor values
- Pressure/temperature protection
- Special abilities (magboots, insulation, etc.)
- Speed modifiers

## Future Expansion
The module system is designed to support:
- Jetpack modules
- Storage expansion modules
- Weapon modules
- Stealth modules
- Medical modules
- Engineering tools
- And more!

## Sprite Requirements
All modsuit sprites should be 32x32 pixels. Each RSI needs:
- **icon.png**: Inventory icon
- **equipped-[SLOT].png**: 4 directional sprite sheet (down, up, right, left)
- **equipped-[SLOT]-sealed.png**: 4 directional sprite sheet for deployed state

The "sealed" state is shown when the suit is deployed and active.

## Technical Notes
- MODsuits use the existing power cell system
- Parts are stored in containers within the control unit
- The system integrates with the inventory system for deployment
- Movement speed modifiers apply based on activation state
- All components are networked for multiplayer support

## Credits
System design based on:
- TGStation's MODsuit system
- Paradise Station's MODsuit implementation
- Monolith Station's sprite structure
