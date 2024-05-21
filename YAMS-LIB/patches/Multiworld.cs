using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class Multiworld
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        var characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");

        // Multiworld stuff
        // Needed variables
        gmData.Scripts.AddScript("mw_debug", """
        var totalString = string(current_hour) + ":" + string(current_minute) + "." + string(current_second) + " - " + argument0
        show_debug_message(totalString);
        exit;
        var f = file_text_open_append(working_directory + "/mw-debug-" + oControl.year + oControl.month + oControl.day + oControl.hour + oControl.minute + ".txt")
        file_text_write_string(f, totalString)
        file_text_writeln(f)
        file_text_close(f)
        """);

        gmData.Code.ByName("gml_Object_oControl_Create_0").PrependGMLInCode($"""
        year = string(current_year)
        month = string(current_month)
        day = string(current_day)
        hour = string(current_hour)
        minute = string(current_minute)
        PACKET_HANDSHAKE=1
        PACKET_UUID=2
        PACKET_LOG=3
        PACKET_KEEP_ALIVE=4
        PACKET_NEW_INVENTORY=5
        PACKET_NEW_LOCATION=6
        PACKET_RECEIVED_PICKUPS=7
        PACKET_GAME_STATE=8
        PACKET_DISPLAY_MESSAGE=9
        PACKET_MALFORMED=10
        socketServer = network_create_server_raw(0, 2016, 1)
        if (socketServer < 0) mw_debug("The port 2016 was not able to be opened!")
        packetNumber = 0
        networkProtocolVersion = 1
        hasConnectedAtLeastOnce = false
        hasConnectedAlpha = 1
        hasConnectedAlphaDirection = 0
        currentGameUuid = "{seedObject.Identifier.WorldUUID}"
        clientState = 0
        CLIENT_DISCONNECTED = 0
        CLIENT_HANDSHAKE_CONNECTION = 1
        CLIENT_FULLY_CONNECTED = 2
        clientSocket = 0
        messageDisplay = ""
        messageDisplayTimer = 0
        MESSAGE_DISPLAY_TIMER_INITIAL = 360
        fetchPickupTimer = -1
        PICKUP_TIMER_INITIAL = 180
        global.lastOffworldNumber = 0
        global.collectedIndices = "locations:"
        global.collectedItems = "items:"
        tempTimer = 0
        mw_debug("Multiworld variables initialized");
        """);

        // Also add the save items to character vars
        characterVarsCode.PrependGMLInCode("""
        global.lastOffworldNumber = 0
        global.collectedIndices = "locations:"
        global.collectedItems = "items:"
        """);

        // Add script to send location and inventory info
        gmData.Scripts.AddScript("send_location_and_inventory_packet", """
        if (oControl.socketServer >= 0 && oControl.clientState >= oControl.CLIENT_FULLY_CONNECTED)
        {
            mw_debug("Sending location+inventory packet:")
            mw_debug("Current locations: " + global.collectedIndices)
            mw_debug("Current inventory: " + global.collectedItems)
            var collectedLocation = buffer_create(512, buffer_grow, 1)
            buffer_seek(collectedLocation, buffer_seek_start, 0)
            buffer_write(collectedLocation, buffer_u8, oControl.PACKET_NEW_LOCATION)
            var currentPos = buffer_tell(collectedLocation)
            buffer_write(collectedLocation, buffer_text, global.collectedIndices)
            var length = (buffer_tell(collectedLocation) - currentPos)
            buffer_seek(collectedLocation, buffer_seek_start, currentPos)
            buffer_write(collectedLocation, buffer_u16, length)
            buffer_write(collectedLocation, buffer_text, global.collectedIndices)
            network_send_raw(oControl.clientSocket, collectedLocation, buffer_get_size(collectedLocation))
            mw_debug("Sent location packet")
            buffer_delete(collectedLocation)
            var collectedItem = buffer_create(512, buffer_grow, 1)
            buffer_seek(collectedItem, buffer_seek_start, 0)
            buffer_write(collectedItem, buffer_u8, oControl.PACKET_NEW_INVENTORY)
            currentPos = buffer_tell(collectedItem)
            buffer_write(collectedItem, buffer_text, global.collectedItems)
            length = (buffer_tell(collectedItem) - currentPos)
            buffer_seek(collectedItem, buffer_seek_start, currentPos)
            buffer_write(collectedItem, buffer_u16, length)
            buffer_write(collectedItem, buffer_text, global.collectedItems)
            network_send_raw(oControl.clientSocket, collectedItem, buffer_get_size(collectedItem))
            mw_debug("Sent inventory packet")
            buffer_delete(collectedItem)
        }
        """);


        // Send collected item and location when collecting items
        gmData.Code.ByName("gml_Object_oItem_Other_10").ReplaceGMLInCode("instance_destroy()", """
        global.collectedIndices += (string(itemid) + ",")
        global.collectedItems += (((string(itemName) + "|") + string(itemQuantity)) + ",")
        mw_debug("Local item was collected: location " + string(itemid) + " , name " + string(itemName) + " quantity " + string(itemQuantity))
        send_location_and_inventory_packet();
        instance_destroy();
        """);

        // Send collected items and locations when loading saves
        gmData.Code.ByName("gml_Object_oLoadGame_Other_10").AppendGMLInCode("""
        mw_debug("Some save got loaded")
        send_room_info_packet(global.start_room);
        send_location_and_inventory_packet();
        """);

        // Send room info when transition rooms
        gmData.Scripts.AddScript("send_room_info_packet", """
        var rm = argument0;
        if (rm == undefined)
            rm = room
        if (oControl.socketServer >= 0 && oControl.clientState >= oControl.CLIENT_FULLY_CONNECTED)
        {
            mw_debug("Sending room info packet, with room " + string(room_get_name(rm)))
            var roomName = buffer_create(512, buffer_grow, 1)
            buffer_seek(roomName, buffer_seek_start, 0)
            buffer_write(roomName, buffer_u8, oControl.PACKET_GAME_STATE)
            var currentPos = buffer_tell(roomName)
            buffer_write(roomName, buffer_text, string(room_get_name(rm)))
            var length = (buffer_tell(roomName) - currentPos)
            buffer_seek(roomName, buffer_seek_start, currentPos)
            buffer_write(roomName, buffer_u16, length)
            buffer_write(roomName, buffer_text, string(room_get_name(rm)))
            network_send_raw(oControl.clientSocket, roomName, buffer_get_size(roomName))
            mw_debug("Sent room packet")
            buffer_delete(roomName)
        }
        """);

        gmData.Code.ByName("gml_Object_oControl_Other_4").AppendGMLInCode("""
        mw_debug("Player has changed their room")
        send_room_info_packet();
        """);

        // Periodically send what our last received offworld item was
        gmData.Code.ByName("gml_Object_oControl_Step_0").AppendGMLInCode("""
        if (socketServer >= 0 && clientState >= oControl.CLIENT_FULLY_CONNECTED && fetchPickupTimer == 0)
        {
            mw_debug("Sending PickupTimer packet. Our current value: " + string(global.lastOffworldNumber));
            fetchPickupTimer = PICKUP_TIMER_INITIAL
            var pickupBuffer = buffer_create(512, buffer_grow, 1)
            buffer_seek(pickupBuffer, buffer_seek_start, 0)
            buffer_write(pickupBuffer, buffer_u8, PACKET_RECEIVED_PICKUPS)
            var currentPos = buffer_tell(pickupBuffer)
            buffer_write(pickupBuffer, buffer_text, string(global.lastOffworldNumber))
            var length = (buffer_tell(pickupBuffer) - currentPos)
            buffer_seek(pickupBuffer, buffer_seek_start, currentPos)
            buffer_write(pickupBuffer, buffer_u16,length)
            buffer_write(pickupBuffer, buffer_text, string(global.lastOffworldNumber))
            network_send_raw(clientSocket, pickupBuffer, buffer_get_size(pickupBuffer))
            mw_debug("send receive pickup packet")
            buffer_delete(pickupBuffer)
        }
        fetchPickupTimer--
        tempTimer++
        if (tempTimer > 9999) tempTimer = 0
        """);

        // Show MW messages + cant open port message
        gmData.Code.ByName("gml_Script_draw_gui").AppendGMLInCode("""
                                             if (oControl.socketServer < 0)
                                             {
                                                 draw_set_font(global.fontGUI2)
                                                 draw_set_halign(fa_left)
                                                 draw_cool_text(0, 50, "Could not open Port!#Please try reopening#the game after 2min.", c_black, c_white, c_white, 1)
                                             }
                                             if (oControl.hasConnectedAtLeastOnce && (oControl.socketServer >= 0 && clientState == oControl.CLIENT_DISCONNECTED))
                                             {
                                                if (oControl.tempTimer % 60 == 0)
                                                    mw_debug("Showing 'game is disconnected' message.")
                                                draw_set_font(global.fontGUI2)
                                                draw_set_halign(fa_left)
                                                draw_sprite_ext(sDisconnected, 0, 5, 61, 1, 1, 0, c_white, oControl.hasConnectedAlpha);
                                                if (oControl.hasConnectedAlpha <= 0)
                                                    oControl.hasConnectedAlphaDirection = 0
                                                else if (oControl.hasConnectedAlpha >= 1.4)
                                                    oControl.hasConnectedAlphaDirection = 1
                                                if (oControl.hasConnectedAlphaDirection == 1)
                                                    oControl.hasConnectedAlpha -= 0.025
                                                else if (oControl.hasConnectedAlphaDirection == 0)
                                                    oControl.hasConnectedAlpha += 0.025
                                                draw_cool_text(15, 50, "Disconnected!", c_black, c_white, c_white, 1)
                                             }
                                             if (global.ingame && oControl.messageDisplay != "" && oControl.messageDisplayTimer > 0)
                                             {
                                                 if (oControl.tempTimer % 60 == 0)
                                                    mw_debug("Showing message display; message: " + string(oControl.messageDisplay) + " timer: " + string(oControl.messageDisplayTimer))
                                                 draw_set_font(global.fontGUI2)
                                                 draw_set_halign(fa_center)
                                                 draw_cool_text(160 + (widescreen_space / 2), 40, oControl.messageDisplay, c_black, c_white, c_white, 1)
                                                 oControl.messageDisplayTimer--
                                                 if (oControl.messageDisplayTimer <= 0)
                                                 {
                                                     oControl.messageDisplayTimer = 0
                                                     messageDisplay = ""
                                                     mw_debug("Timer ran out, resetting both display and timer to empty values")
                                                 }
                                                 draw_set_halign(fa_left)
                                                 draw_set_font(global.fontMenuTiny)
                                             }
                                             """);

        // Received network packet event.
        var oControlNetworkReceived = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_Object_oControl_Other_68") };
        UndertaleCodeLocals locals = new UndertaleCodeLocals();
        locals.Name = oControlNetworkReceived.Name;
        UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
        argsLocal.Name = gmData.Strings.MakeString("arguments");
        argsLocal.Index = 0;
        locals.Locals.Add(argsLocal);
        oControlNetworkReceived.LocalsCount = 1;
        gmData.CodeLocals.Add(locals);
        oControlNetworkReceived.SubstituteGMLCode($$"""
        mw_debug("Async Networking event entered")
        var type_event, _buffer, bufferSize, msgid, handshake, socket, malformed, protocolVer, length, currentPos, i, upperLimit;
        type_event = ds_map_find_value(async_load, "type")
        switch type_event
        {
            case 1:
                mw_debug("The client has done the initial connection to our socket")
                clientSocket = ds_map_find_value(async_load, "socket")
                break
            case 2:
                mw_debug("The client disconnected from the socket, resetting values")
                clientSocket = 0
                clientState = CLIENT_DISCONNECTED
                packetNumber = 0
                fetchPickupsTimer = -1
                break
            case 3:
                mw_debug("Client send us incoming data")
                socket = clientSocket
                if (socket == undefined || socket == 0)
                    exit
                mw_debug("Client socket is valid")
                mw_debug(("Socket: " + string(socket)))
                _buffer = ds_map_find_value(async_load, "buffer")
                if (_buffer == undefined)
                    exit
                mw_debug("Confirmed for Client to send us valid data")
                mw_debug(("Buffer id: " + string(_buffer)))
                bufferSize = buffer_get_size(_buffer)
                mw_debug("Buffer size: " + string(bufferSize))
                msgid = buffer_read(_buffer, buffer_u8)
                mw_debug(("Message ID of the packet: " + string(msgid)))
                switch msgid
                {
                    case PACKET_HANDSHAKE:
                        mw_debug("Entered Handshake handling.")
                        mw_debug(("Current client state: " + string(clientState)))
                        if (clientState == CLIENT_DISCONNECTED)
                        {
                            mw_debug("Client is not connected, initiating procedure")
                            clientState = CLIENT_HANDSHAKE_CONNECTION
                            mw_debug("Client has been internally set to initial connection")
                            var handshake = buffer_create(2, buffer_grow, 1)
                            buffer_seek(handshake, buffer_seek_start, 0)
                            buffer_write(handshake, buffer_u8, PACKET_HANDSHAKE)
                            buffer_write(handshake, buffer_u8, string(packetNumber))
                            network_send_raw(socket, handshake, buffer_get_size(handshake))
                            buffer_delete(handshake)
                            packetNumber = ((packetNumber + 1) % 256)
                            mw_debug("Internal packet number set to " + string(packetNumber))
                        }
                        else mw_debug("Client tried to initiate handshake while already connected")
                        break
                    case PACKET_UUID:
                        mw_debug("Client requested UUID")
                        if (clientState < CLIENT_HANDSHAKE_CONNECTION)
                        {
                            mw_debug("Client tried to request UUID while not being connected yet.")
                            exit
                        }
                        mw_debug("Client passed the handshake check")
                        protocolVer = buffer_create(1024, buffer_grow, 1)
                        buffer_seek(protocolVer, buffer_seek_start, 0)
                        buffer_write(protocolVer, buffer_u8, PACKET_UUID)
                        buffer_write(protocolVer, buffer_u8, string(packetNumber))
                        currentPos = buffer_tell(protocolVer)
                        buffer_write(protocolVer, buffer_text, string(networkProtocolVersion) + "," + string(currentGameUuid))
                        length = (buffer_tell(protocolVer) - currentPos)
                        buffer_seek(protocolVer, buffer_seek_start, currentPos)
                        buffer_write(protocolVer, buffer_u16, length)
                        buffer_write(protocolVer, buffer_text, string(networkProtocolVersion) + "," + string(currentGameUuid))
                        network_send_raw(socket, protocolVer, buffer_get_size(protocolVer))
                        mw_debug("Sent protocol packet; api version " + string(networkProtocolVersion) + " uuid " + string(currentGameUuid))
                        buffer_delete(protocolVer)
                        packetNumber = ((packetNumber + 1) % 256)
                        clientState = CLIENT_FULLY_CONNECTED
                        hasConnectedAtLeastOnce = true
                        mw_debug("Client has been connected at least once set, Client is fully connected now. Packet number: " + string(packetNumber))
                        fetchPickupTimer = PICKUP_TIMER_INITIAL
                        mw_debug("Pickup timer has been set to the initial timer")
                        send_room_info_packet();
                        send_location_and_inventory_packet();
                        break
                    case PACKET_DISPLAY_MESSAGE:
                        mw_debug("Client send us a message to show")
                        var tempMessage = buffer_read(_buffer, buffer_string)
                        var upperLimit = 45
                        if widescreen
                            upperLimit = 50
                        if (string_length(tempMessage) > upperLimit)
                            tempMessage = string_insert("-#", tempMessage, upperLimit)
                        messageDisplay = tempMessage
                        messageDisplayTimer = MESSAGE_DISPLAY_TIMER_INITIAL
                        mw_debug("Message that client wants us to show: " + messageDisplay)
                        break
                    case PACKET_RECEIVED_PICKUPS:
                        mw_debug("Client send us pickups that we should receive")
                        var message = buffer_read(_buffer, buffer_string)
                        mw_debug("Pickups are in the following message: " + string(message))
                        var splitBy = "|"
                        var splitted;
                        var i = 0
                        var total = string_count(splitBy, message)
                        while (i<=total)
                        {
                            var limit = string_length(message)+1
                            if (string_count(splitBy, message) > 0)
                                limit = string_pos("|", message)
                            var element = string_copy(message, 1, limit-1)
                            splitted[i] = element
                            message = string_copy(message, limit+1, string_length(message)-limit)
                            i++
                        }

                        var provider = splitted[0]
                        itemName = splitted[1]      // needs to be an instance variable due to some scripts expecting it to be.
                        var model = splitted[2]
                        var quantity = real(splitted[3])
                        var remoteItemNumber = real(splitted[4])
                        mw_debug("prov:" + provider + " itemName:" + itemName + " model: " + model + " quant: " + string(quantity) + " remote num: " + string(remoteItemNumber))
                        if (remoteItemNumber <= global.lastOffworldNumber)
                        {
                            mw_debug("We have already received this item. Bailing out. Our current item is " + string(global.lastOffworldNumber))
                            break;
                        }
                        var knownItem = true
                        active = true       // workaround for some scripts
                        switch (itemName)
                        {
                            case "{{ItemEnum.EnergyTank.GetEnumMemberValue()}}":
                                get_etank()
                                break
                            case "{{ItemEnum.MissileExpansion.GetEnumMemberValue()}}":
                                get_missile_expansion(quantity)
                                break
                            case "{{ItemEnum.MissileLauncher.GetEnumMemberValue()}}":
                                get_missile_launcher(quantity)
                                break
                            case "{{ItemEnum.SuperMissileExpansion.GetEnumMemberValue()}}":
                                get_super_missile_expansion(quantity)
                                break
                            case "{{ItemEnum.SuperMissileLauncher.GetEnumMemberValue()}}":
                                get_super_missile_launcher(quantity)
                                break
                            case "{{ItemEnum.PBombExpansion.GetEnumMemberValue()}}":
                                get_pb_expansion(quantity)
                                break
                            case "{{ItemEnum.PBombLauncher.GetEnumMemberValue()}}":
                                get_pb_launcher(quantity)
                                break
                            case "{{ItemEnum.Bombs.GetEnumMemberValue()}}":
                                get_bombs()
                                break
                            case "{{ItemEnum.Powergrip.GetEnumMemberValue()}}":
                                get_power_grip()
                                break
                            case "{{ItemEnum.Spiderball.GetEnumMemberValue()}}":
                                get_spider_ball()
                                break
                            case "{{ItemEnum.Springball.GetEnumMemberValue()}}":
                                get_spring_ball()
                                break
                            case "{{ItemEnum.Screwattack.GetEnumMemberValue()}}":
                                get_screw_attack()
                                break
                            case "{{ItemEnum.Varia.GetEnumMemberValue()}}":
                                get_varia()
                                break
                            case "{{ItemEnum.Spacejump.GetEnumMemberValue()}}":
                                get_space_jump()
                                break
                            case "{{ItemEnum.Speedbooster.GetEnumMemberValue()}}":
                                get_speed_booster()
                                break
                            case "{{ItemEnum.Hijump.GetEnumMemberValue()}}":
                                get_hijump()
                                break
                            case "{{ItemEnum.ProgressiveJump.GetEnumMemberValue()}}":
                                get_progressive_jump()
                                break
                            case "{{ItemEnum.Gravity.GetEnumMemberValue()}}":
                                get_gravity()
                                break
                            case "{{ItemEnum.ProgressiveSuit.GetEnumMemberValue()}}":
                                get_progressive_suit()
                                break
                            case "{{ItemEnum.Charge.GetEnumMemberValue()}}":
                                get_charge_beam()
                                break
                            case "{{ItemEnum.Ice.GetEnumMemberValue()}}":
                                get_ice_beam()
                                break
                            case "{{ItemEnum.Wave.GetEnumMemberValue()}}":
                                get_wave_beam()
                                break
                            case "{{ItemEnum.Spazer.GetEnumMemberValue()}}":
                                get_spazer_beam()
                                break
                            case "{{ItemEnum.Plasma.GetEnumMemberValue()}}":
                                get_plasma_beam()
                                break
                            case "{{ItemEnum.Morphball.GetEnumMemberValue()}}":
                                get_morph_ball()
                                break
                            case "{{ItemEnum.SmallHealthDrop.GetEnumMemberValue()}}":
                                get_small_health_drop(quantity);
                                break
                            case "{{ItemEnum.BigHealthDrop.GetEnumMemberValue()}}":
                                get_big_health_drop(quantity)
                                break
                            case "{{ItemEnum.MissileDrop.GetEnumMemberValue()}}":
                                get_missile_drop(quantity)
                                break
                            case "{{ItemEnum.SuperMissileDrop.GetEnumMemberValue()}}":
                                get_super_missile_drop(quantity);
                                break
                            case "{{ItemEnum.PBombDrop.GetEnumMemberValue()}}":
                                get_power_bomb_drop(quantity)
                                break
                            case "{{ItemEnum.Flashlight.GetEnumMemberValue()}}":
                                get_flashlight(quantity);
                                break
                            case "{{ItemEnum.Blindfold.GetEnumMemberValue()}}":
                                get_blindfold(quantity);
                                break
                            case "{{ItemEnum.SpeedBoosterUpgrade.GetEnumMemberValue()}}":
                                get_speed_booster_upgrade(quantity)
                                break
                            case "{{ItemEnum.WalljumpBoots.GetEnumMemberValue()}}":
                                get_walljump_upgrade()
                                break
                            case "{{ItemEnum.InfiniteBombJump.GetEnumMemberValue()}}":
                                get_IBJ_upgrade()
                                break
                            case "{{ItemEnum.LongBeam.GetEnumMemberValue()}}":
                                get_long_beam()
                                break
                            default:
                                if (string_count("Metroid DNA", itemName) > 0)
                                {
                                    get_dna()
                                    break;
                                }
                                mw_debug("Have gotten unknown Item.")
                                knownItem = false
                                break;
                        }
                        var tempMessage = "Received " + itemName + " from " + provider
                        if (!knownItem)
                            tempMessage = "Unknown item from " + provider + "! Please report this!!!"
                        var upperLimit = 45
                        if widescreen
                            upperLimit = 50
                        if (string_length(tempMessage) > upperLimit)
                            tempMessage = string_insert("-#", tempMessage, upperLimit)
                        messageDisplay = tempMessage
                        messageDisplayTimer = MESSAGE_DISPLAY_TIMER_INITIAL
                        mw_debug("Message that we'll be showing: " + messageDisplay)
                        if (knownItem)
                        {
                            mw_debug("Item that we received was known to us. Attempting to increase inventory; current: " + string(global.collectedItems))
                            global.collectedItems += (itemName + "|" + string(quantity) + ",")
                            global.lastOffworldNumber = remoteItemNumber
                            mw_debug("Inventory and lastoffworld set. offworld: " + string(global.lastOffworldNumber) + " inventory: " + string(global.collectedItems))
                        }
                        send_location_and_inventory_packet()
                        active = false
                        itemName = undefined
                        mw_debug("End of Pickup receive handling.")
                        break;
                }
                break
            default:
                mw_debug("Unknown message id. Sending malformed packet as response")
                malformed = buffer_create(1024, buffer_grow, 1)
                buffer_seek(malformed, buffer_seek_start, 0)
                buffer_write(malformed, buffer_u8, PACKET_MALFORMED)
                buffer_write(malformed, buffer_u8, 0)
                network_send_raw(socket, malformed, buffer_get_size(malformed))
                buffer_delete(malformed)
                break
        }
        mw_debug("Leaving async networking event")
        """);
        gmData.Code.Add(oControlNetworkReceived);
        var oControlCollisionList = gmData.GameObjects.ByName("oControl").Events[7];
        var oControlAction = new UndertaleGameObject.EventAction();
        oControlAction.CodeId = oControlNetworkReceived;
        var oControlEvent = new UndertaleGameObject.Event();
        oControlEvent.EventSubtype = 68;
        oControlEvent.Actions.Add(oControlAction);
        oControlCollisionList.Add(oControlEvent);
    }
}
