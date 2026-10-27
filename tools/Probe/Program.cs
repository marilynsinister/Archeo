using System.Reflection;
using System.Runtime.Loader;
string game = @"D:\Games\Steam\steamapps\common\Stardew Valley";
if(args.Contains("--museum-api"))
{
    using var image=Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(game,"Stardew Valley.dll"));
    foreach(var query in new[]{("StardewValley.Utility","ForEachLocation|GetAllLocations"),("StardewValley.Object","questItem|isLostItem"),("StardewValley.Item","modData|Stack"),("StardewValley.NPC","GetBoundingBox")})
    {
        var type=image.MainModule.GetType(query.Item1);
        foreach(var method in type.Methods.Where(p=>p.IsPublic&&System.Text.RegularExpressions.Regex.IsMatch(p.Name,query.Item2)))Console.WriteLine(method.FullName+" / "+string.Join(",",method.Parameters.Select(p=>p.Name)));
        foreach(var field in type.Fields.Where(p=>p.IsPublic&&System.Text.RegularExpressions.Regex.IsMatch(p.Name,query.Item2)))Console.WriteLine(field.FullName);
    }
    return;
}
if(args.Contains("--frontier-api"))
{
    using var image=Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(game,"Stardew Valley.dll"));
    foreach(var name in new[]{"StardewValley.Locations.BusStop","StardewValley.Locations.BoatTunnel","StardewValley.Locations.IslandSouth"})
    {
        var type=image.MainModule.GetType(name);
        foreach(var method in type.Methods.Where(p=>p.HasBody))
        foreach(var instruction in method.Body.Instructions.Where(p=>p.OpCode.Code==Mono.Cecil.Cil.Code.Ldstr && System.Text.RegularExpressions.Regex.IsMatch(p.Operand?.ToString()??"","Vault|Boat|boat|Island|island|Bus|bus")))
            Console.WriteLine(method.Name+": "+instruction.Operand);
    }
    foreach(var name in new[]{"StardewValley.Buff","StardewValley.Buffs.BuffEffects","StardewValley.Buffs.BuffManager","StardewValley.Farmer"})
    {
        var type=image.MainModule.GetType(name);
        foreach(var method in type.Methods.Where(p=>p.IsPublic && (p.IsConstructor && name!="StardewValley.Farmer" || p.Name=="applyBuff" || name.Contains("BuffManager"))))Console.WriteLine(method.FullName);
        if(name.Contains("Buff"))foreach(var field in type.Fields.Where(p=>p.IsPublic))Console.WriteLine(field.FullName);
    }
    return;
}
if(args.Contains("--excavation-api"))
{
    using var image=Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(game,"Stardew Valley.dll"));
    using var smapi=Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(game,"StardewModdingAPI.dll"));
    foreach(var instruction in smapi.MainModule.GetType("StardewModdingAPI.Context").Methods.Single(p=>p.Name=="get_IsPlayerFree").Body.Instructions)Console.WriteLine(instruction);
    foreach(var query in new[]{("StardewValley.Tools.Hoe","DoFunction|tilesAffected"),("StardewValley.Tool","tilesAffected"),("StardewValley.TerrainFeatures.TerrainFeature","isPassable"),("StardewValley.Farmer","CurrentTool|CurrentItem")})
    {
        var type=image.MainModule.GetType(query.Item1);
        foreach(var method in type.Methods.Where(p=>System.Text.RegularExpressions.Regex.IsMatch(p.Name,query.Item2)))
        {
            Console.WriteLine(method.FullName);
            if(type.Name=="Hoe" && method.Name=="DoFunction") foreach(var instruction in method.Body.Instructions) Console.WriteLine(instruction);
        }
    }
    return;
}
if(args.Contains("--journal-api"))
{
    using var image=Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(game,"Stardew Valley.dll"));
    foreach(var query in new[]{("StardewValley.Menus.TextBox","Selected|Text|Update|Draw|ctor|Width|Height|X|Y"),("StardewValley.Game1","showTextEntry|keyboardDispatcher"),("StardewValley.Menus.IClickableMenu","receiveGamePadButton|cleanupBeforeExit")})
    {
        var type=image.MainModule.GetType(query.Item1);
        foreach(var method in type.Methods.Where(p=>p.IsPublic && System.Text.RegularExpressions.Regex.IsMatch(p.Name,query.Item2))) Console.WriteLine(method.FullName);
        foreach(var field in type.Fields.Where(p=>p.IsPublic && System.Text.RegularExpressions.Regex.IsMatch(p.Name,query.Item2))) Console.WriteLine(field.FullName);
    }
    using var smapi=Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(game,"StardewModdingAPI.dll"));
    foreach(var field in smapi.MainModule.GetType("StardewModdingAPI.SButton").Fields.Where(p=>p.Name.StartsWith("Controller"))) Console.WriteLine(field.Name);
    return;
}
if(args.Contains("--verify-residents"))
{
    using var image=Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(game,"Stardew Valley.dll"));
    bool HasMethod(string type,string name,string result,params string[] parameters)
        => image.MainModule.GetType(type).Methods.Any(m=>m.IsPublic && m.Name==name && m.ReturnType.FullName==result
            && m.Parameters.Select(p=>p.ParameterType.FullName).SequenceEqual(parameters));
    if(!HasMethod("StardewValley.Dialogue",".ctor","System.Void","StardewValley.NPC","System.String","System.String")
        || !HasMethod("StardewValley.Menus.DialogueBox",".ctor","System.Void","StardewValley.Dialogue")
        || !HasMethod("StardewValley.Farmer","changeFriendship","System.Void","System.Int32","StardewValley.NPC")
        || !HasMethod("StardewValley.NPC","get_CanSocialize","System.Boolean")
        || !image.MainModule.GetType("StardewValley.NPC").Fields.Any(p=>p.Name=="isSleeping" && p.IsPublic && p.FieldType.FullName=="Netcode.NetBool"))
        throw new Exception("Resident portrait dialogue, friendship or availability API changed.");
    Console.WriteLine("PASS: standalone portrait dialogue constructors, friendship award API, social availability and synchronized sleeping flag. No game process was started.");
    return;
}
if(args.Contains("--residents"))
{
    using var image=Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(game,"Stardew Valley.dll"));
    foreach(var query in new[]{("StardewValley.Dialogue",".ctor"),("StardewValley.Game1","drawDialogue"),("StardewValley.Farmer","changeFriendship"),("StardewValley.NPC","get_displayName|CanSocialize|Sleeping|sleeping"),("StardewValley.Menus.DialogueBox",".ctor")})
    {
        var type=image.MainModule.GetType(query.Item1);
        foreach(var method in type.Methods.Where(p=>p.IsPublic && System.Text.RegularExpressions.Regex.IsMatch(p.Name,query.Item2)))Console.WriteLine(method.FullName);
        foreach(var field in type.Fields.Where(p=>p.IsPublic && System.Text.RegularExpressions.Regex.IsMatch(p.Name,query.Item2)))Console.WriteLine(field.FullName);
        if(type.Name=="Dialogue")
        foreach(var method in type.Methods.Where(p=>p.IsConstructor && p.Parameters.Count==3 && p.Parameters[2].ParameterType.FullName=="System.String"))
        {foreach(var parameter in method.Parameters)Console.WriteLine(parameter.Name);foreach(var instruction in method.Body.Instructions)Console.WriteLine(instruction);}
    }
    return;
}
if(args.Contains("--verify-nature"))
{
    using var image=Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(game,"Stardew Valley.dll"));
    var location=image.MainModule.GetType("StardewValley.GameLocation");
    if(!location.Methods.Any(p=>p.Name=="IsSnowingHere" && p.IsPublic && p.Parameters.Count==0 && p.ReturnType.FullName=="System.Boolean"))
        throw new Exception("Snow observation API changed.");
    var date=image.MainModule.GetType("StardewValley.WorldDate");
    foreach(string name in new[]{"Year","SeasonIndex","DayOfMonth"})
        if(!date.Methods.Any(p=>p.Name=="get_"+name && p.IsPublic && p.ReturnType.FullName=="System.Int32"))throw new Exception("Calendar API changed: "+name);
    var night=image.MainModule.GetType("StardewValley.Events.SoundInTheNightEvent");
    bool found=false;
    foreach(var method in night.Methods.Where(p=>p.HasBody))
    {
        var body=method.Body.Instructions;
        foreach(int index in Enumerable.Range(0,body.Count).Where(i=>body[i].Operand?.ToString()?.Contains("ResourceClump::.ctor")==true))
            found|=body.Skip(Math.Max(0,index-12)).Take(Math.Min(12,index)).Any(p=>p.OpCode.Code==Mono.Cecil.Cil.Code.Ldc_I4 && Equals(p.Operand,622));
    }
    if(!found)throw new Exception("Meteor clump ID changed; recheck observation.");
    foreach(string typeName in new[]{"StardewValley.TerrainFeatures.LargeTerrainFeature","StardewValley.TerrainFeatures.Bush"})
    {
        var type=image.MainModule.GetType(typeName);
        foreach(var method in type.Methods.Where(p=>p.Name.Contains("BoundingBox")))Console.WriteLine(method.FullName);
    }
    Console.WriteLine("PASS: snow and calendar observation APIs; vanilla meteor event creates resource clump 622. No game process was started.");
    return;
}
if (args.Contains("--nature"))
{
    using var image=Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(game,"Stardew Valley.dll"));
    foreach(var query in new[]{("StardewValley.WorldDate","Season|Year|Day"),("StardewValley.GameLocation","Snow|resourceClumps|isTileLocationOpen"),("StardewValley.TerrainFeatures.ResourceClump","tile|Tile|parentSheetIndex|width|height|BoundingBox"),("StardewValley.Buildings.Building","occup|Intersects|BoundingBox"),("StardewValley.Game1","get_Date")})
    {
        var type=image.MainModule.GetType(query.Item1);
        Console.WriteLine(query.Item1);
        foreach(var field in type.Fields.Where(f=>f.IsPublic && System.Text.RegularExpressions.Regex.IsMatch(f.Name,query.Item2)))Console.WriteLine(field.FullName);
        foreach(var method in type.Methods.Where(m=>m.IsPublic && System.Text.RegularExpressions.Regex.IsMatch(m.Name,query.Item2)))Console.WriteLine(method.FullName);
        if(type.Name=="GameLocation")foreach(var instruction in type.Methods.Single(m=>m.Name=="isTileLocationOpen" && m.Parameters[0].ParameterType.FullName=="Microsoft.Xna.Framework.Vector2").Body.Instructions)Console.WriteLine(instruction);
    }
    foreach(var type in image.MainModule.Types.Where(t=>t.Name.Contains("SoundInTheNight")))
    foreach(var method in type.Methods.Where(m=>m.HasBody))
    {
        var body=method.Body.Instructions;
        foreach(int at in Enumerable.Range(0,body.Count).Where(i=>body[i].Operand?.ToString()?.Contains("ResourceClump::.ctor")==true))
        {Console.WriteLine(method.FullName);foreach(var instruction in body.Skip(Math.Max(0,at-15)).Take(20))Console.WriteLine(instruction);}
    }
    return;
}
if (args.Contains("--verify-unusual"))
{
    using var image = Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(game, "Stardew Valley.dll"));
    var tree = image.MainModule.GetType("StardewValley.TerrainFeatures.FruitTree");
    foreach (string name in new[]{"TryAddFruit","IsInSeasonHere"})
        if (!tree.Methods.Any(m=>m.Name==name && m.IsPublic && !m.IsStatic && m.Parameters.Count==0 && m.ReturnType.FullName=="System.Boolean"))
            throw new Exception("Fruit tree API changed: "+name);
    foreach (string name in new[]{"fruit","daysUntilMature","growthStage","stump","struckByLightningCountdown"})
        if (!tree.Fields.Any(f=>f.Name==name && f.IsPublic)) throw new Exception("Fruit tree field changed: "+name);
    var grow=tree.Methods.Single(m=>m.Name=="TryAddFruit");
    if (!grow.Body.Instructions.Any(i=>i.OpCode.Code==Mono.Cecil.Cil.Code.Ldc_I4_3)) throw new Exception("Vanilla fruit capacity changed; recheck flute rules.");
    Console.WriteLine("PASS: public fruit-tree growth and season APIs, synchronized fields, and the installed three-fruit capacity. No game process was started.");
    return;
}
if (args.Contains("--fruit"))
{
    using var image = Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(game, "Stardew Valley.dll"));
    var fruit = image.MainModule.GetType("StardewValley.TerrainFeatures.FruitTree");
    foreach (var field in fruit.Fields.Where(f=>System.Text.RegularExpressions.Regex.IsMatch(f.Name,"fruit|Fruit|Mature|mature|stump|Lightning|treeId"))) Console.WriteLine(field.FullName);
    foreach (var method in fruit.Methods.Where(m=>System.Text.RegularExpressions.Regex.IsMatch(m.Name,"Fruit|Season|shake|GetData|ctor")))
    {
        Console.WriteLine(method.FullName);
        if (method.Name is "TryAddFruit" or "IsInSeasonHere" or "GetFruitId" or "GetData")
            foreach (var instruction in method.Body.Instructions) Console.WriteLine(instruction);
    }
    return;
}
if (args.Contains("--verify-shipwreck"))
{
    using var image = Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(game, "Stardew Valley.dll"));
    var obj = image.MainModule.GetType("StardewValley.Object");
    var action = obj.Methods.SingleOrDefault(m => m.Name == "checkForAction" && m.ReturnType.FullName == "System.Boolean"
        && string.Join(",",m.Parameters.Select(p=>p.ParameterType.FullName)) == "StardewValley.Farmer,System.Boolean");
    if (action == null) throw new Exception("Ship bell action hook signature changed.");
    var crystal = image.MainModule.Types.SelectMany(t=>t.Methods).Where(m=>m.HasBody)
        .FirstOrDefault(m=>m.Body.Instructions.Any(i=>i.OpCode.Code==Mono.Cecil.Cil.Code.Ldstr && i.Operand?.ToString()=="crystal"));
    if (crystal == null) throw new Exception("The crystal sound cue is no longer used in installed game code.");
    Console.WriteLine("PASS: declared Object.checkForAction(Farmer,bool) matches the bell hook; installed game uses the crystal sound cue in " + crystal.FullName + ". No game process was started.");
    return;
}
if (args.Contains("--verify-gallery"))
{
    using var image = Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(game, "Stardew Valley.dll"));
    var furniture = image.MainModule.GetType("StardewValley.Objects.Furniture");
    void Require(bool value, string message) { if (!value) throw new Exception(message); }
    foreach (var contract in new[] {
        ("clicked", "System.Boolean", "StardewValley.Farmer"),
        ("checkForAction", "System.Boolean", "StardewValley.Farmer,System.Boolean"),
        ("canBeRemoved", "System.Boolean", "StardewValley.Farmer"),
        ("draw", "System.Void", "Microsoft.Xna.Framework.Graphics.SpriteBatch,System.Int32,System.Int32,System.Single")
    })
        Require(furniture.Methods.Count(m => m.Name == contract.Item1 && m.ReturnType.FullName == contract.Item2
            && string.Join(",",m.Parameters.Select(p => p.ParameterType.FullName)) == contract.Item3) == 1, "Furniture hook contract changed: " + contract.Item1);
    var obj = image.MainModule.GetType("StardewValley.Object");
    Require(obj.Methods.Any(m => m.Name == "performToolAction" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "StardewValley.Tool"), "Tool protection target changed");
    Require(obj.Fields.Any(f => f.Name == "heldObject" && f.FieldType.FullName.Contains("NetRef`1<StardewValley.Object>")), "Contained object is no longer a network field");
    var team = image.MainModule.GetType("StardewValley.FarmerTeam");
    Require(team.Methods.Any(m => m.Name == "Update" && m.HasBody && m.Body.Instructions.Any(i => i.Operand?.ToString()?.Contains("globalInventoryMutexes") == true)), "Team no longer updates global mutexes");
    Require(team.Methods.Any(m => m.Name == "GetOrCreateGlobalInventoryMutex" && m.IsPublic), "Team mutex factory unavailable");
    var chest = image.MainModule.GetType("StardewValley.Objects.Chest");
    Require(chest.Methods.Any(m => m.IsConstructor && m.IsPublic && m.Parameters.Count == 2 && m.Parameters[0].ParameterType.FullName == "System.Boolean" && m.Parameters[1].IsOptional), "Vanilla player chest constructor changed");
    Console.WriteLine("PASS: installed game metadata matches all 5 museum hook signatures, networked held-object storage, chest constructor, and team mutex updates. No game process was started.");
    return;
}
if (args.Contains("--gallery-audit"))
{
    using var image = Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(game, "Stardew Valley.dll"));
    var furniture = image.MainModule.GetType("StardewValley.Objects.Furniture");
    foreach (var method in furniture.Methods.Where(p => p.HasBody && (p.Body.Instructions.Any(i => i.Operand?.ToString()?.Contains("heldObject") == true) || p.Name.Contains("clicked"))))
    {
        Console.WriteLine(method.FullName);
        if (method.Name is "clicked" or "performObjectDropInAction" or "getOne" or "DayUpdate" or "actionOnPlayerEntryOrPlacement" || method.Name.Contains("getOne"))
            foreach (var instruction in method.Body.Instructions) Console.WriteLine(instruction);
    }
    return;
}
if (args.Contains("--gallery"))
{
    using var image = Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(game, "Stardew Valley.dll"));
    foreach (var query in new[] {
        ("StardewValley.Objects.Furniture", "canBeRemoved|checkForAction|drawAtNonTileSpot"),
        ("StardewValley.Object", "performToolAction"),
        ("StardewValley.FarmerTeam", "GetOrCreateGlobalInventoryMutex|Update"),
        ("StardewValley.Network.NetMutex", ".*")
    })
    {
        var type = image.MainModule.GetType(query.Item1);
        foreach (var method in type.Methods.Where(p => System.Text.RegularExpressions.Regex.IsMatch(p.Name, "^(" + query.Item2 + ")$")))
        {
            Console.WriteLine(method.FullName);
            if (!method.HasBody) continue;
            foreach (var instruction in method.Body.Instructions)
                if (query.Item1.Contains("FarmerTeam") && method.Name != "GetOrCreateGlobalInventoryMutex")
                {
                    if (instruction.Operand?.ToString()?.Contains("Mutex") == true) Console.WriteLine(instruction);
                }
                else Console.WriteLine(instruction);
        }
    }
    return;
}
if (args.Contains("--furniture"))
{
    using var image = Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(game, "Stardew Valley.dll"));
    var furniture = image.MainModule.GetType("StardewValley.Objects.Furniture");
    foreach (var method in furniture.Methods.Where(p => p.Name == "draw" && p.Parameters.Count == 4))
    {
        Console.WriteLine(method.FullName);
        var body = method.Body.Instructions;
        var indices = body.Select((instruction, index) => (instruction,index)).Where(p => p.instruction.Operand?.ToString()?.Contains("heldObject") == true).Select(p=>p.index).ToList();
        foreach (var index in Enumerable.Range(0,body.Count).Where(i => indices.Any(j => i >= j-8 && i < j+80)).Distinct())
            Console.WriteLine(body[index]);
    }
    return;
}
AssemblyLoadContext.Default.Resolving += (_, name) => {
    foreach (var dir in new[] { game, Path.Combine(game, "smapi-internal") }) {
        var path = Path.Combine(dir, name.Name + ".dll");
        if (File.Exists(path)) return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
    }
    return null;
};
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(game, "Stardew Valley.dll"));
foreach (var query in new[] {
    ("StardewValley.GameLocation", "digUpArtifactSpot|isTileLocation|isTilePassable|isTileOnMap|isTileOccupied|doesTileHaveProperty|isWaterTile"),
    ("StardewValley.Farmer", "Tile|addItem|couldInventory|CurrentItem|Stamina|FacingDirection"),
    ("StardewValley.Menus.IClickableMenu", "drawTextureBox|gameWindowSizeChanged|receiveKeyPress|receiveGamePadButton|drawMouse"),
    ("StardewValley.Game1", "loadForNewGame|parseDebugInput|Exit|newDay|gameMode|player|currentLocation|locations"),
    ("StardewValley.Tools.Hoe", "DoFunction"),
    ("StardewValley.NPC", "checkAction|draw|Dialogue"),
    ("StardewValley.Objects.Furniture", "heldObject|draw|checkForAction|placementAction|canBeRemoved|attemptRemoval|performToolAction|boundingBox|drawPosition|mutex|TileLocation"),
    ("StardewValley.Objects.Chest", "Items|items|mutex|GetItems|GetActualCapacity|SpecialChestType|ChestType|ClearNulls|.ctor"),
    ("StardewValley.GameLocation", "furniture|Furniture|GetLocation|NameOrUniqueName"),
    ("StardewValley.Dialogue", ".ctor"),
    ("StardewValley.Farmer", "getStandingPosition|Items|removeItemsFromInventory|hasItemInInventory|addItem"),
    ("StardewValley.ItemRegistry", "Create"),
    ("StardewValley.Buff", ".ctor")
}) {
    var type = assembly.GetType(query.Item1)!;
    Console.WriteLine(query.Item1);
    foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(m => System.Text.RegularExpressions.Regex.IsMatch(m.Name, query.Item2))) Console.WriteLine("  " + member);
}
