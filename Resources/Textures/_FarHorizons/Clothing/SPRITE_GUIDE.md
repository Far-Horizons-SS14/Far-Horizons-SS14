# MODsuit Sprite Guide

## What You Need to Create

For each modsuit, you need **5 complete RSI folders** with sprites:

### 1. Control Unit (Back/Modsuits/yoursuit.rsi)
- `icon.png` - 32x32 item icon (what you see in inventory)
- `equipped-BACKPACK.png` - 128x32 sprite sheet (4 directions: down, up, right, left)
- `equipped-BACKPACK-sealed.png` - 128x32 sprite sheet (deployed state)

### 2. Helmet (Head/Modsuits/yoursuit.rsi)
- `icon.png` - 32x32 item icon
- `equipped-HELMET.png` - 128x32 sprite sheet (4 directions)
- `equipped-HELMET-sealed.png` - 128x32 sprite sheet (deployed state)

### 3. Chestplate (OuterClothing/Modsuits/yoursuit.rsi)
- `icon.png` - 32x32 item icon
- `equipped-OUTERCLOTHING.png` - 128x32 sprite sheet (4 directions)
- `equipped-OUTERCLOTHING-sealed.png` - 128x32 sprite sheet (deployed state)

### 4. Gauntlets (Hands/Modsuits/yoursuit.rsi)
- `icon.png` - 32x32 item icon
- `equipped-HAND.png` - 128x32 sprite sheet (4 directions)
- `equipped-HAND-sealed.png` - 128x32 sprite sheet (deployed state)

### 5. Boots (Shoes/Modsuits/yoursuit.rsi)
- `icon.png` - 32x32 item icon
- `equipped-FEET.png` - 128x32 sprite sheet (4 directions)
- `equipped-FEET-sealed.png` - 128x32 sprite sheet (deployed state)

## Sprite Sheet Format

Each equipped sprite is a **128x32 sprite sheet** with 4 frames:
```
[Frame 1: Down] [Frame 2: Up] [Frame 3: Right] [Frame 4: Left]
   32x32           32x32         32x32           32x32
```

## Sealed vs Unsealed States

- **Unsealed**: When the suit is retracted/off (more open, relaxed)
- **Sealed**: When the suit is deployed/on (closed helmet, armor plates extended, sealed joints)

Think of "sealed" as "combat ready" or "EVA mode" - everything closed up tight.

## Color Scheme Suggestions

Based on TG/Paradise modsuits:
- **Basic/Civilian**: Gray/white, simple design
- **Engineering**: Yellow/orange with hazard stripes
- **Atmospheric**: White/cyan with thermal elements
- **Security**: Red/black with armor plating
- **Medical**: White/blue with cross symbols
- **Mining**: Brown/yellow, rugged look
- **Syndicate**: Red/black, tactical appearance
- **Research**: Purple/white, sleek design

## Where to Put Sprites

Once you create your sprites, place them in:
```
Resources/Textures/_FarHorizons/Clothing/
├── Back/Modsuits/yoursuit.rsi/
├── Head/Modsuits/yoursuit.rsi/
├── OuterClothing/Modsuits/yoursuit.rsi/
├── Hands/Modsuits/yoursuit.rsi/
└── Shoes/Modsuits/yoursuit.rsi/
```

## Reference Examples

Look at these existing sprite structures for reference:
- `Resources/Textures/_FarHorizons/Clothing/OuterClothing/Hardsuits/` (similar format)
- Monolith Station's modsuit sprites (linked in research)
- Basic modsuit folders (created as templates)

## Tips for Spriting

1. **Start with the chestplate** - it's the most visible part
2. **Keep the design consistent** across all parts
3. **Use the sealed state** to show the suit "powering up" or "armoring up"
4. **Add glow effects** or lights in the sealed state to show it's active
5. **Make the control unit** look like a backpack/power pack
6. **Consider species variants** if you want (Vox, Tajaran, etc.)

## Current Status

The folder structure is ready:
- ✅ All RSI folders created
- ✅ meta.json files generated
- ❌ PNG sprites needed (you need to create these!)

**Next step**: Create the actual PNG sprites and place them in the appropriate folders!

## Minimum Viable Sprites

If you want to test the system without full sprites, create simple placeholder PNGs:
- 32x32 colored squares for icons
- 128x32 sprite sheets with 4 identical colored squares
- Make them different colors to tell parts apart

Then you can test the deployment/activation mechanics while working on better sprites!
