if not modules then modules = { } end modules ['back-shp'] = {
    version   = 1.001,
    comment   = "companion to lpdf-ini.mkiv",
    author    = "Hans Hagen, PRAGMA-ADE, Hasselt NL",
    copyright = "PRAGMA ADE / ConTeXt Development Team",
    license   = "see context related readme files"
}

local type, next = type, next
local round = math.round

local setmetatableindex = table.setmetatableindex
local formatters        = string.formatters
local concat            = table.concat
local keys              = table.keys
local sortedhash        = table.sortedhash
local splitstring       = string.split
local idiv              = number.idiv
local extract           = bit32.extract
local nuts              = nodes.nuts

local tonut             = nodes.tonut
local tonode            = nodes.tonode

local getdirection      = nuts.getdirection
local getlist           = nuts.getlist
local getoffsets        = nuts.getoffsets
local getorientation    = nuts.getorientation
local getfield          = nuts.getfield
local getwhd            = nuts.getwhd
local getkern           = nuts.getkern
local getheight         = nuts.getheight
local getdepth          = nuts.getdepth
local getwidth          = nuts.getwidth
local getnext           = nuts.getnext
local getsubtype        = nuts.getsubtype
local getid             = nuts.getid
local getleader         = nuts.getleader
local getglue           = nuts.getglue
local getshift          = nuts.getshift
local getdata           = nuts.getdata
local getboxglue        = nuts.getboxglue
local getexpansion      = nuts.getexpansion

local setdirection      = nuts.setdirection
local setfield          = nuts.setfield
local setlink           = nuts.setlink

local isglyph           = nuts.isglyph
local findtail          = nuts.tail
local nextdir           = nuts.traversers.dir
local nextnode          = nuts.traversers.node

local rangedimensions   = nuts.rangedimensions
local effectiveglue     = nuts.effective_glue

local nodecodes         = nodes.nodecodes
local whatsitcodes      = nodes.whatsitcodes
local leadercodes       = nodes.leadercodes
local subtypes          = nodes.subtypes

local dircodes          = nodes.dircodes
local normaldir_code    = dircodes.normal

local dirvalues         = nodes.dirvalues
local lefttoright_code  = dirvalues.lefttoright
local righttoleft_code  = dirvalues.righttoleft

local glyph_code        = nodecodes.glyph
local kern_code         = nodecodes.kern
local glue_code         = nodecodes.glue
local hlist_code        = nodecodes.hlist
local vlist_code        = nodecodes.vlist
local dir_code          = nodecodes.dir
local disc_code         = nodecodes.disc
local math_code         = nodecodes.math
local rule_code         = nodecodes.rule
local marginkern_code   = nodecodes.marginkern
local whatsit_code      = nodecodes.whatsit

local leader_code       = leadercodes.leader
local cleader_code      = leadercodes.cleader
local xleader_code      = leadercodes.xleader
local gleader_code      = leadercodes.gleader

local saveposwhatsit_code     = whatsitcodes.savepos
local userdefinedwhatsit_code = whatsitcodes.userdefined
local openwhatsit_code        = whatsitcodes.open
local writewhatsit_code       = whatsitcodes.write
local closewhatsit_code       = whatsitcodes.close
local lateluawhatsit_code     = whatsitcodes.latelua
local literalwhatsit_code     = whatsitcodes.literal
local setmatrixwhatsit_code   = whatsitcodes.setmatrix
local savewhatsit_code        = whatsitcodes.save
local restorewhatsit_code     = whatsitcodes.restore

local texget            = tex.get

local fontdata          = fonts.hashes.identifiers
local characters        = fonts.hashes.characters
local parameters        = fonts.hashes.parameters

local trace_missing     = false

local report            = logs.reporter("drivers")
local report_missing    = logs.reporter("drivers","missing")
----- report_detail     = logs.reporter("drivers","detail")

local getpagedimensions  getpagedimensions = function()
    getpagedimensions = backends.codeinjections.getpagedimensions
    return getpagedimensions()
end

local instances = { }

drivers = {
    instances   = instances,
    lmtxversion = 0.10,
}

local defaulthandlers = {
    -- housekeeping
    prepare           = function() end,
    initialize        = function() end,
    finalize          = function() end,
    updatefontstate   = function() end,
    wrapup            = function() end,
    -- handlers
    pushorientation   = function() if trace_missing then report("pushorientation")   end end,
    poporientation    = function() if trace_missing then report("poporientation")    end end,
    flushcharacter    = function() if trace_missing then report("flushcharacter")    end end,
    flushrule         = function() if trace_missing then report("flushrule")         end end,
    flushlatelua      = function() if trace_missing then report("flushlatelua")      end end,
    flushpdfliteral   = function() if trace_missing then report("flushpdfliteral")   end end,
    flushpdfsetmatrix = function() if trace_missing then report("flushpdfsetmatrix") end end,
    flushpdfsave      = function() if trace_missing then report("flushpdfsave")      end end,
    flushpdfrestore   = function() if trace_missing then report("flushpdfrestore")   end end,
    flushspecial      = function() if trace_missing then report("flushspecial")      end end,
    flushpdfimage     = function() if trace_missing then report("flushpdfimage")     end end,
}

function drivers.install(specification)
    local name = specification.name
    if not name then
        report("missing driver name")
        return
    end
    local actions = specification.actions
    if not actions then
        report("no actions for driver %a",name)
        return
    end
    local flushers = specification.flushers
    if not flushers then
        report("no flushers for driver %a",name)
        return
    end
    setmetatableindex(flushers,defaulthandlers)
    instances[name] = specification
end

function drivers.prepare(name)
    local driver = instances[name]
    if not driver then
        return
    end
    local prepare = driver.actions.prepare
    if prepare then
        prepare()
    end
end

function drivers.wrapup(name)
    local driver = instances[name]
    if not driver then
        return
    end
    local wrapup = driver.actions.wrapup
    if wrapup then
        wrapup()
    end
end

---------------------------------------------------------------------------------------

local synctex          = false

local lastfont         = nil
local fontcharacters   = nil

local magicconstants   = tex.magicconstants
local trueinch         = magicconstants.trueinch
local maxdimen         = magicconstants.maxdimen
local running          = magicconstants.running

local pos_h            = 0
local pos_v            = 0
local pos_r            = lefttoright_code
local shippingmode     = "none"

local abs_max_v        = 0
local abs_max_h        = 0

local shipbox_h        = 0
local shipbox_v        = 0
local page_size_h      = 0
local page_size_v      = 0
----- page_h_origin    = 0 -- trueinch
----- page_v_origin    = 0 -- trueinch

local initialize
local finalize
local updatefontstate
local pushorientation
local poporientation
local flushcharacter
local flushrule
local flushpdfliteral
local flushpdfsetmatrix
local flushpdfsave
local flushpdfrestore
local flushlatelua
local flushspecial
local flushpdfimage

-- make local

function drivers.getpos () return round(pos_h), round(pos_v) end
function drivers.gethpos() return round(pos_h)               end
function drivers.getvpos() return               round(pos_v) end

local function synch_pos_with_cur(ref_h, ref_v, h, v)
    if pos_r == righttoleft_code then
        pos_h = ref_h - h
    else
        pos_h = ref_h + h
    end
    pos_v = ref_v - v
end

-- characters

local flush_character

local stack   = setmetatableindex("table")
local level   = 0
local nesting = 0
local main    = 0

-- todo: cache streams

local function flush_vf_packet(pos_h,pos_v,pos_r,font,char,data,factor,vfcommands)

    if nesting > 100 then
        return
    elseif nesting == 0 then
        main = font
    end

    nesting = nesting + 1

    local saved_h = pos_h
    local saved_v = pos_v
    local saved_r = pos_r
            pos_r = lefttoright_code

    local data  = fontdata[font]
    local fnt   = font
    local fonts = data.fonts
    local siz   = (data.parameters.factor or 1)/65536

    local function flushchar(font,char,fnt,chr,f,e)
        if fnt then
            local nest = char ~= chr or font ~= fnt
            if fnt == 0 then
                fnt = main
            end
            return flush_character(false,fnt,chr,factor,nest,pos_h,pos_v,pos_r,f,e)
        else
            return 0
        end
    end

    -- we assume resolved fonts: id mandate but maybe also size

    for i=1,#vfcommands do
        local packet  = vfcommands[i]
        local command = packet[1]
        if command == "char" then
            local chr, f, e = packet[2], packet[3], packet[4]
            pos_h = pos_h + flushchar(font,char,fnt,chr,f,e)
        elseif command == "slot" then
            local index, chr, f, e = packet[2], packet[3], packet[4], packet[5]
            if index == 0 then
                pos_h = pos_h + flushchar(font,char,font,chr,f,e)
            else
                local okay = fonts and fonts[index]
                if okay then
                    local fnt = okay.id
                    if fnt then
                        pos_h = pos_h + flushchar(font,char,fnt,chr,f,e)
                    end
                end
            end
        elseif command == "right" then
            local h = packet[2] -- * siz
            if factor ~= 0 and h ~= 0 then
                h = h + h * factor / 1000 -- expansion
            end
            pos_h = pos_h + h
        elseif command == "down" then
            local v = packet[2] -- * siz
            pos_v = pos_v - v
        elseif command == "push" then
            level = level + 1
            local s = stack[level]
            s[1] = pos_h
            s[2] = pos_v
        elseif command == "pop" then
            if level > 0 then
                local s = stack[level]
                pos_h = s[1]
                pos_v = s[2]
                level = level - 1
            end
        elseif command == "pdf" then
            flushpdfliteral(false,pos_h,pos_v,packet[2],packet[3])
        elseif command == "rule" then
            local size_v = packet[2]
            local size_h = packet[3]
            if factor ~= 0 and size_h > 0 then
                size_h = size_h + size_h * factor / 1000
            end
            if size_h > 0 and size_v > 0 then
                flushsimplerule(pos_h,pos_v,pos_r,size_h,size_v)
                pos_h = pos_h + size_h
            end
        elseif command == "font" then
            local index = packet[2]
            local okay  = fonts and fonts[index]
            if okay then
                fnt = okay.id or fnt -- or maybe just return
            end
        elseif command == "lua" then
            local code = packet[2]
            if type(code) ~= "function" then
                code = loadstring(code)
            end
            if type(code) == "function" then
                code(font,char,pos_h,pos_v)
            end
        elseif command == "node" then
            hlist_out(packet[2])
        elseif command == "image" then
            -- doesn't work because intercepted by engine so we use a different
            -- mechanism (for now)
            local image = packet[2]
            -- to do
        elseif command == "pdfmode" then
            -- doesn't happen
     -- elseif command == "special" then
     --     -- not supported
     -- elseif command == "nop" then
     --     -- nothing to do|
     -- elseif command == "scale" then
     --     -- not supported
        end
    end

    pos_h = saved_h
    pos_v = saved_v
    pos_r = saved_r

 -- synch_pos_with_cur(ref_h, ref_v, pos_h,pos_v)
    nesting = nesting - 1
end

flush_character = function(current,font,char,factor,vfcommands,pos_h,pos_v,pos_r,f,e)

    if font ~= lastfont then
        lastfont       = font
        fontcharacters = characters[font]
        updatefontstate(font)
    end

    local data = fontcharacters[char]
    if not data then
     -- print("missing font/char",font,char)
     -- lua_glyph_not_found_callback(font,char)
        return 0, 0, 0
    end
    local width, height, depth
    if current then
        width, height, depth = getwhd(current)
        factor = getexpansion(current)
        if factor and factor ~= 0 then
            width = (1.0 + factor/1000000.0) * width
        end
    else
        width  = data.width or 0
        height = data.height or 0
        depth  = data.depth or 0
        if not factor then
            factor = 0
        end
    end
    if pos_r == righttoleft_code then
        pos_h = pos_h - width
    end
    if vfcommands then
        vfcommands = data.commands
    end
    if vfcommands then
        flush_vf_packet(pos_h,pos_v,pos_r,font,char,data,factor,vfcommands) -- also f ?
    else
        local orientation = data.orientation
        if orientation and (orientation == 1 or orientation == 3) then
            local x, y = data.xoffset, data.yoffset
            if x then
                pos_h = pos_h + x
            end
            if y then
                pos_v = pos_v + y
            end
            pushorientation(orientation,pos_h,pos_v)
            flushcharacter(current,pos_h,pos_v,pos_r,font,char,data,factor,width,f,e)
            poporientation(orientation,pos_h,pos_v)
        else
            flushcharacter(current,pos_h,pos_v,pos_r,font,char,data,factor,width,f,e)
        end
    end
    return width, height, depth
end

-- end of characters

local function reset_state()
    pos_h         = 0
    pos_v         = 0
    pos_r         = lefttoright_code
    shipbox_h     = 0
    shipbox_v     = 0
    shippingmode  = "none"
    page_size_h   = 0
    page_size_v   = 0
 -- page_h_origin = 0 -- trueinch
 -- page_v_origin = 0 -- trueinch
end

local function dirstackentry(t,k)
    local v = {
        cur_h = 0,
        cur_v = 0,
        ref_h = 0,
        ref_v = 0,
    }
    t[k] = v
    return v
end

local dirstack = setmetatableindex(dirstackentry)

local function reset_dir_stack()
    dirstack = setmetatableindex(dirstackentry)
end

local function open_write_file(n)
--     -- can't yet be done
--     local stream = getfield(current,"stream")
--     local path   = getfield(current,"area")
--     local name   = getfield(current,"name")
--     local suffix = getfield(current,"ext")
--     if suffix == "" then
--         name = name .. ".tex"
--     else
--         name = name .. suffix
--     end
--     if path ~= "" then
--         name = file.join(path,name)
--     end
end

local function close_write_file(n)
    -- can't yet be done
end

local function write_to_file(current)
    local stream = getfield(current,"stream")
    local data   = getdata(current)
    texio.write_nl(stream,data)
end

local function wrapup_leader(current,subtype)
    if subtype == writewhatsit_code then
        write_to_file(current)
    elseif subtype == closewhatsit_code then
        close_write_file(current)
    elseif subtype == openwhatsit_code then
        open_write_file(current)
    end
end

local hlist_out, vlist_out  do

    -- effective_glue :
    --
    -- local cur_g      = 0
    -- local cur_glue   = 0.0
    --
    -- local width, stretch, shrink, stretch_order, shrink_order = getglue(current)
    -- if g_sign == 1 then -- stretching
    --     if stretch_order == g_order then
    --      -- rule_wd  = width - cur_g
    --      -- cur_glue = cur_glue + stretch
    --      -- cur_g    = g_set * cur_glue
    --      -- rule_wd  = rule_wd + cur_g
    --         rule_ht = width + g_set * stretch
    --     else
    --         rule_wd  = width
    --     end
    -- else
    --     if shrink_order == g_order then
    --      -- rule_wd  = width - cur_g
    --      -- cur_glue = cur_glue - shrink
    --      -- cur_g    = g_set * cur_glue
    --      -- rule_wd  = rule_wd + cur_g
    --         rule_ht = width - g_set * shrink
    --     else
    --         rule_wd  = width
    --     end
    -- end

    local function applyanchor(orientation,x,y,width,height,depth,woffset,hoffset,doffset,xoffset,yoffset)
        local ot = extract(orientation, 0,4)
        local ay = extract(orientation, 4,4)
        local ax = extract(orientation, 8,4)
        local of = extract(orientation,12,4)
        if ot == 4 then
            ot, ay = 0, 1
        elseif ot == 5 then
            ot, ay = 0, 2
        end
        if ot == 0 or ot == 2 then
            if     ax == 1 then x = x - width
            elseif ax == 2 then x = x + width
            elseif ax == 3 then x = x - width/2
            elseif ax == 4 then x = x + width/2
            end
            if ot == 2 then
                doffset, hoffset = hoffset, doffset
            end
            if     ay == 1 then y = y - doffset
            elseif ay == 2 then y = y + hoffset
            elseif ay == 3 then y = y + (doffset + hoffset)/2 - doffset
            end
        elseif ot == 1 or ot == 3 then
            if     ay == 1 then y = y - height
            elseif ay == 2 then y = y + height
            elseif ay == 3 then y = y - height/2
            end
            if ot == 1 then
                doffset, hoffset = hoffset, doffset
            end
            if     ax == 1 then x = x - width
            elseif ax == 2 then x = x + width
            elseif ax == 3 then x = x - width/2
            elseif ax == 4 then x = x + width/2
            elseif ax == 5 then x = x - hoffset
            elseif ax == 6 then x = x + doffset
            end
        end
        return ot, x + xoffset, y - yoffset
    end

    -- rangedir can stick to widths only

    rangedimensions = node.direct.naturalwidth or rangedimensions

    local function calculate_width_to_enddir(this_box,begindir) -- can be a helper
        local dir_nest = 1
        local enddir   = begindir
        for current, subtype in nextdir, getnext(begindir) do
            if subtype == normaldir_code then -- todo
                dir_nest = dir_nest + 1
            else
                dir_nest = dir_nest - 1
            end
            if dir_nest == 0 then -- does the type matter
                enddir = current
                local width = rangedimensions(this_box,begindir,enddir)
                return enddir, width
            end
        end
        if enddir == begindir then
            local width = rangedimensions(this_box,begindir) -- ,enddir)
            return enddir, width
        end
        return enddir, 0
    end

    -- check frequencies of nodes

    hlist_out = function(this_box,current)
        local outer_doing_leaders = false

        local ref_h = pos_h
        local ref_v = pos_v
        local ref_r = pos_r
              pos_r = getdirection(this_box)

        local boxwidth,
              boxheight,
              boxdepth   = getwhd(this_box)
        local g_set,
              g_order,
              g_sign     = getboxglue(this_box)

        local cur_h      = 0
        local cur_v      = 0

        if not current then
            current = getlist(this_box)
        end

     -- if synctex then
     --     synctexhlist(this_box)
     -- end

        while current do
            local char, id = isglyph(current)
            if char then
                local x_offset, y_offset = getoffsets(current)
                if x_offset ~= 0 or y_offset ~= 0 then
                    synch_pos_with_cur(ref_h, ref_v, cur_h + x_offset, cur_v - y_offset)
                end
                local wd, ht, dp = flush_character(current,id,char,false,true,pos_h,pos_v,pos_r)
                cur_h = cur_h + wd
            elseif id == glue_code then
                local rule_wd, rule_ht, rule_dp
                if g_sign == 0 then
                    rule_wd = getwidth(current)
                else
                    rule_wd = effectiveglue(current,this_box)
                end
                local leader = getleader(current)
                if leader then
                    local leader_wd = 0
                    local boxdir = getdirection(leader) or lefttoright_code
                    local width, height, depth = getwhd(leader)
                    if getid(leader) == rule_code then
                        rule_ht = height
                        rule_dp = depth
                        if rule_ht == running then
                            rule_ht = boxheight
                        end
                        if rule_dp == running then
                            rule_dp = boxdepth
                        end
                        local left, right = getoffsets(leader)
                        if left ~= 0 then
                            rule_ht = rule_ht - left
                            pos_v   = pos_v + left
                        end
                        if right ~= 0 then
                            rule_dp = rule_dp - right
                        end
                        local total = rule_ht + rule_dp
                        if total > 0 and rule_wd > 0  then
                            local size_h = rule_wd
                            local size_v = total
                            if pos_r == righttoleft_code then
                                pos_h  = pos_h - size_h
                            end
                            pos_v = pos_v - rule_dp
                            flushrule(current,pos_h,pos_v,pos_r,size_h,size_v)
                        end
                        cur_h = cur_h + rule_wd
                    else
                        leader_wd = width
                        if leader_wd > 0 and rule_wd > 0 then
                            rule_wd = rule_wd + 10
                            local edge = cur_h + rule_wd
                            local lx = 0
                            local subtype = getsubtype(current)
                            if subtype == gleader_code then
                                local save_h = cur_h
                                if pos_r == righttoleft_code then
                                    cur_h = ref_h - shipbox_h - cur_h
                                    cur_h = leader_wd * (cur_h / leader_wd)
                                    cur_h = ref_h - shipbox_h - cur_h
                                else
                                    cur_h = cur_h + ref_h - shipbox_h
                                    cur_h = leader_wd * (cur_h / leader_wd)
                                    cur_h = cur_h - ref_h - shipbox_h
                                end
                                if cur_h < save_h then
                                    cur_h = cur_h + leader_wd
                                end
                            elseif subtype == leader_code then -- aleader ?
                                local save_h = cur_h
                                cur_h = leader_wd * (cur_h / leader_wd)
                                if cur_h < save_h then
                                    cur_h = cur_h + leader_wd
                                end
                            else
                                lq = rule_wd / leader_wd
                                lr = rule_wd % leader_wd
                                if subtype == cleader_code then
                                    cur_h = cur_h + lr / 2
                                else
                                    lx = lr / (lq + 1)
                                    cur_h = cur_h + (lr - (lq - 1) * lx) / 2
                                end
                            end
                            while cur_h + leader_wd <= edge do
                                local basepoint_h = 0
                                local basepoint_v = getshift(leader)
                                if boxdir ~= pos_r then
                                    basepoint_h = boxwidth
                                end
                                synch_pos_with_cur(ref_h,ref_v,cur_h + basepoint_h,basepoint_v)
                                outer_doing_leaders = doing_leaders
                                doing_leaders       = true
                                if getid(leader) == vlist_code then
                                    vlist_out(leader)
                                else
                                    hlist_out(leader)
                                end
                                doing_leaders = outer_doing_leaders
                                cur_h = cur_h + leader_wd + lx
                            end
                            cur_h = edge - 10
                        else
                            cur_h = cur_h + rule_wd
                         -- if synctex then
                         --     synch_pos_with_cur(ref_h, ref_v, cur_h, cur_v)
                         --     synctexhorizontalruleorglue(p, this_box)
                         -- end
                        end
                    end
                else
                    cur_h = cur_h + rule_wd
                 -- if synctex then
                 --     synch_pos_with_cur(ref_h, ref_v, cur_h, cur_v)
                 --     synctexhorizontalruleorglue(p, this_box)
                 -- end
                end
            elseif id == hlist_code or id == vlist_code then
                -- w h d dir l, s, o = getall(current)
                local boxdir = getdirection(current) or lefttoright_code
                local width, height, depth = getwhd(current)
                local list = getlist(current)
                if list then
                    local shift, orientation = getshift(current)
                    if not orientation then
                        local basepoint_h = boxdir ~= pos_r and width or 0
                     -- local basepoint_v = shift
                        synch_pos_with_cur(ref_h,ref_v,cur_h + basepoint_h,shift)
                        if id == vlist_code then
                            vlist_out(current,list)
                        else
                            hlist_out(current,list)
                        end
                    elseif orientation == 0x1000 then
                        local orientation, xoffset, yoffset = getorientation(current)
                        local basepoint_h = boxdir ~= pos_r and width or 0
                     -- local basepoint_v = shift
                        synch_pos_with_cur(ref_h,ref_v,cur_h + basepoint_h + xoffset,shift - yoffset)
                        if id == vlist_code then
                            vlist_out(current,list)
                        else
                            hlist_out(current,list)
                        end
                    else
                        local orientation, xoffset, yoffset, woffset, hoffset, doffset = getorientation(current)
                        local orientation, basepoint_h, basepoint_v = applyanchor(orientation,0,shift,width,height,depth,woffset,hoffset,doffset,xoffset,yoffset)
                        if orientation == 1 then
                            basepoint_h = basepoint_h + doffset
                            if boxdir == pos_r then
                                basepoint_v = basepoint_v - height
                            end
                        elseif orientation == 2 then
                            if boxdir == pos_r then
                                basepoint_h = basepoint_h + width
                            end
                        elseif orientation == 3 then
                            basepoint_h = basepoint_h + hoffset
                            if boxdir ~= pos_r then
                                basepoint_v = basepoint_v - height
                            end
                        end
                        synch_pos_with_cur(ref_h,ref_v,cur_h+basepoint_h,cur_v + basepoint_v)
                        pushorientation(orientation,pos_h,pos_v,pos_r)
                        if id == vlist_code then
                            vlist_out(current,list)
                        else
                            hlist_out(current,list)
                        end
                        poporientation(orientation,pos_h,pos_v,pos_r)
                    end
                else
                 -- if synctex then
                 --     if id == vlist_code then
                 --         synctexvoidvlist(p,this_box)
                 --     else
                 --         synctexvoidhlist(p,this_box)
                 --     end
                 -- end
                end
                cur_h = cur_h + width
            elseif id == disc_code then
                local replace = getfield(current,"replace")
                if replace and getsubtype(current) ~= select_disc then
                    -- we could flatten .. no gain
                    setlink(findtail(replace),getnext(current))
                    setlink(current,replace)
                    setfield(current,"replace")
                end
            elseif id == kern_code then
             -- if synctex then
             --     synctexkern(p, this_box)
             -- end
                local kern   = getkern(current)
                local factor = getexpansion(current)
                if factor and factor ~= 0 then
                    cur_h = cur_h + (1.0 + factor/1000000.0) * kern
                else
                    cur_h = cur_h + kern
                end
            elseif id == rule_code then
                -- w h d dir l r = getall(current)
                local ruledir = getdirection(current)
                local width, height, depth = getwhd(current)
                local left, right = getoffsets(current)
                if not ruledir then
                    ruledir = pos_r
                    setdirection(current,ruledir)
                end
                if height == running then
                    height = boxheight
                end
                if depth == running then
                    depth = boxdepth
                end
                if left ~= 0 then
                    height = height - left
                    pos_v  = pos_v + left
                end
                if right ~= 0 then
                    depth = depth - right
                end
                local total = height + depth
                if total > 0 and width > 0  then
                    local size_h = width
                    local size_v = total
                    if pos_r == righttoleft_code then
                        pos_h  = pos_h - size_h
                    end
                    pos_v  = pos_v - depth
                    flushrule(current,pos_h,pos_v,pos_r,size_h,size_v)
                end
                cur_h = cur_h + width
            elseif id == math_code then
             -- if synctex then
             --     synctexmath(p, this_box)
             -- end
                local kern = getkern(current)
                if kern ~= 0 then
                    cur_h = cur_h + kern
                elseif g_sign == 0 then
                    rule_wd = getwidth(current)
                else
                    rule_wd = effectiveglue(current,this_box)
                end
            elseif id == dir_code then
                -- we normally have proper begin-end pairs
                -- a begin without end is (silently) handled
                -- an end without a begin will be (silently) skipped
                -- we only need to move forward so a faster calculation
                local dir, cancel = getdirection(current)
                if cancel then
                    local ds = dirstack[current]
                    ref_h = ds.ref_h
                    ref_v = ds.ref_v
                    cur_h = ds.cur_h
                    cur_v = ds.cur_v
                    pos_r = dir
                else
                    local enddir, width = calculate_width_to_enddir(this_box,current)
                    local ds = dirstack[enddir]
                    ds.cur_h = cur_h + width
                    if dir ~= pos_r then
                        cur_h = ds.cur_h
                    end
                    if enddir ~= current then
                        local ds = dirstack[enddir]
                        ds.cur_v = cur_v
                        ds.ref_h = ref_h
                        ds.ref_v = ref_v
                        setdirection(enddir,pos_r)
                    end
                    synch_pos_with_cur(ref_h,ref_v,cur_h,cur_v)
                    ref_h = pos_h
                    ref_v = pos_v
                    cur_h = 0
                    cur_v = 0
                    pos_r = dir
                end
            elseif id == whatsit_code then
                local subtype = getsubtype(current)
                if subtype == literalwhatsit_code then
                    flushpdfliteral(current,pos_h,pos_v)
                elseif subtype == lateluawhatsit_code then
                    flushlatelua(current,pos_h,pos_v,cur_h,cur_v)
                elseif subtype == setmatrixwhatsit_code then
                    flushpdfsetmatrix(current,pos_h,pos_v)
                elseif subtype == savewhatsit_code then
                    flushpdfsave(current,pos_h,pos_v)
                elseif subtype == restorewhatsit_code then
                    flushpdfrestore(current,pos_h,pos_v)
             -- elseif subtype == special_code then
             --     flushspecial(current,pos_h,pos_v)
                elseif subtype == saveposwhatsit_code then
                    last_position_x = pos_h -- or pdf_h
                    last_position_y = pos_v -- or pdf_v
                elseif subtype == userdefinedwhatsit_code then
                    -- nothing
                elseif subtype == openwhatsit_code or subtype == writewhatsit_code or subtype == closewhatsit_code then
                    if not doing_leaders then
                        wrapup_leader(current,subtype)
                    end
                end
            elseif id == marginkern_code then
                cur_h = cur_h + getkern(current)
            end
            current = getnext(current)
            synch_pos_with_cur(ref_h, ref_v, cur_h, cur_v) -- already done in list so skip
        end
     -- if synctex then
     --     synctextsilh(this_box)
     -- end
        pos_h = ref_h
        pos_v = ref_v
        pos_r = ref_r
    end

    vlist_out = function(this_box,current)
        local outer_doing_leaders = false

        local ref_h = pos_h
        local ref_v = pos_v
        local ref_r = pos_r
              pos_r = getdirection(this_box)

        local boxwidth,
              boxheight,
              boxdepth   = getwhd(this_box)
        local g_set,
              g_order,
              g_sign     = getboxglue(this_box)

        local cur_h      = 0
        local cur_v      = - boxheight

        local top_edge   = cur_v

        synch_pos_with_cur(ref_h, ref_v, cur_h, cur_v)

        if not current then
            current = getlist(this_box)
        end

     -- if synctex then
     --     synctexvlist(this_box)
     -- end

     -- while current do
     --     local id = getid(current)
        for current, id, subtype in nextnode, current do
            if id == glue_code then
                local rule_wd, rule_ht, rule_dp
                if g_sign == 0 then
                    rule_ht = getwidth(current)
                else
                    rule_ht = effectiveglue(current,this_box)
                end
                local leader = getleader(current)
                if leader then
                    local width, height, depth = getwhd(leader)
                    if getid(leader) == rule_code then
                        rule_wd = width
                        rule_dp = 0
                        if rule_wd == running then
                            rule_wd = boxwidth
                        end
                        local left, right = getoffsets(leader)
                        if left ~= 0 then
                            rule_wd = rule_wd - left
                            cur_h   = cur_h + left
                        end
                        if right ~= 0 then
                            rule_wd = rule_wd - right
                        end
                        local total = rule_ht + rule_dp
                        if total > 0 and rule_wd > 0 then
                            local size_h = rule_wd
                            local size_v = total
                            if pos_r == righttoleft_code then
                                cur_h  = cur_h - size_h
                            end
                            cur_v  = cur_v - size_v
                            flushrule(current,pos_h,pos_v - size_v,pos_r,size_h,size_v)
                        end
                        cur_v = cur_v + total
                    else
                        local leader_ht = height + depth
                        if leader_ht > 0 and rule_ht > 0 then
                            rule_ht = rule_ht + 10
                            local edge = cur_v + rule_ht
                            local ly   = 0
                         -- local subtype = getsubtype(current)
                            if subtype == gleader_code then
                                save_v = cur_v
                                cur_v  = ref_v - shipbox_v - cur_v
                                cur_v  = leader_ht * (cur_v / leader_ht)
                                cur_v  = ref_v - shipbox_v - cur_v
                                if cur_v < save_v then
                                    cur_v = cur_v + leader_ht
                                end
                            elseif subtype == leader_code then -- aleader
                                save_v = cur_v
                                cur_v = top_edge + leader_ht * ((cur_v - top_edge) / leader_ht)
                                if cur_v < save_v then
                                    cur_v = cur_v + leader_ht
                                end
                            else
                                lq = rule_ht / leader_ht
                                lr = rule_ht % leader_ht
                                if subtype == cleaders_code then
                                    cur_v = cur_v + lr / 2
                                else
                                    ly = lr / (lq + 1)
                                    cur_v = cur_v + (lr - (lq - 1) * ly) / 2
                                end
                            end
                            while cur_v + leader_ht <= edge do
                                synch_pos_with_cur(ref_h, ref_v, getshift(leader), cur_v + height)
                                outer_doing_leaders = doing_leaders
                                doing_leaders = true
                                if getid(leader) == vlist_code then
                                    vlist_out(leader)
                                else
                                    hlist_out(leader)
                                end
                                doing_leaders = outer_doing_leaders
                                cur_v = cur_v + leader_ht + ly
                            end
                            cur_v = edge - 10
                        else
                            cur_v = cur_v + rule_ht
                        end
                    end
                else
                    cur_v = cur_v + rule_ht
                end
            elseif id == hlist_code or id == vlist_code then
                -- w h d dir l, s, o = getall(current)
                local boxdir = getdirection(current) or lefttoright_code
                local width, height, depth = getwhd(current)
                local list = getlist(current)
                if list then
                    local shift, orientation = getshift(current)
                    if not orientation then
                     -- local basepoint_h = shift
                     -- local basepoint_v = height
                        if boxdir ~= pos_r then
                            shift = shift + width
                        end
                        synch_pos_with_cur(ref_h,ref_v,shift,cur_v + height)
                        if id == vlist_code then
                            vlist_out(current,list)
                        else
                            hlist_out(current,list)
                        end
                    elseif orientation == 0x1000 then
                        local orientation, xoffset, yoffset = getorientation(current)
                     -- local basepoint_h = shift
                     -- local basepoint_v = height
                        if boxdir ~= pos_r then
                            shift = shift + width
                        end
                        synch_pos_with_cur(ref_h,ref_v,shift + xoffset,cur_v + height - yoffset)
                        if id == vlist_code then
                            vlist_out(current,list)
                        else
                            hlist_out(current,list)
                        end
                    else
                        local orientation, xoffset, yoffset, woffset, hoffset, doffset = getorientation(current)
                        local orientation, basepoint_h, basepoint_v = applyanchor(orientation,shift,height,width,height,depth,woffset,hoffset,doffset,xoffset,yoffset)
                        if orientation == 1 then
                            basepoint_h = basepoint_h + width - height
                            basepoint_v = basepoint_v - height
                        elseif orientation == 2 then
                            basepoint_h = basepoint_h + width
                            basepoint_v = basepoint_v + depth - height
                        elseif orientation == 3 then -- weird
                            basepoint_h = basepoint_h + height
                        end
                        synch_pos_with_cur(ref_h,ref_v,basepoint_h,cur_v + basepoint_v)
                        pushorientation(orientation,pos_h,pos_v,pos_r)
                        if id == vlist_code then
                            vlist_out(current,list)
                        else
                            hlist_out(current,list)
                        end
                        poporientation(orientation,pos_h,pos_v,pos_r)
                    end
                else
                 -- if synctex then
                 --     synch_pos_with_cur(ref_h, ref_v, cur_h, cur_v)
                 --     if id == vlist_code then
                 --         synctexvoidvlist(current, this_box)
                 --     else
                 --         synctexvoidhlist(current, this_box)
                 --     end
                 -- end
                end
                cur_v = cur_v + height + depth
            elseif id == kern_code then
                cur_v = cur_v + getkern(current)
            elseif id == rule_code then
                -- w h d dir l r = getall(current)
                local ruledir = getdirection(current)
                local width, height, depth = getwhd(current)
                local left, right = getoffsets(current)
                if not ruledir then
                    ruledir = pos_r
                    setdirection(current,ruledir)
                end
                if width == running then
                    width = boxwidth
                end
                if left ~= 0 then
                    width = width - left
                    cur_h = cur_h + left
                end
                if right ~= 0 then
                    width = width - right
                end
                local total = height + depth
                if total > 0 and width > 0 then
                    local size_h = width
                    local size_v = total
                    if pos_r == righttoleft_code then
                        cur_h  = cur_h - size_h
                    end
                    flushrule(current,pos_h,pos_v - size_v,pos_r,size_h,size_v)
                end
                cur_v = cur_v + total
            elseif id == whatsit_code then
             -- local subtype = getsubtype(current)
                if subtype == literalwhatsit_code then
                    flushpdfliteral(current,pos_h,pos_v)
                elseif subtype == lateluawhatsit_code then
                    flushlatelua(current,pos_h,pos_v,cur_h,cur_v)
                elseif subtype == setmatrixwhatsit_code then
                    flushpdfsetmatrix(current,pos_h,pos_v)
                elseif subtype == savewhatsit_code then
                    flushpdfsave(current,pos_h,pos_v)
                elseif subtype == restorewhatsit_code then
                    flushpdfrestore(current,pos_h,pos_v)
                elseif subtype == saveposwhatsit_code then
                    last_position_x = pos_h -- or pdf_h
                    last_position_y = pos_v -- or pdf_v
                elseif subtype == openwhatsit_code or subtype == writewhatsit_code or subtype == closewhatsit_code then
                    if not doing_leaders then
                        wrapup_leader(current,subtype)
                    end
             -- elseif subtype == special_code then
             --     flushspecial(current,pos_h,pos_v)
                end
            end
         -- current = getnext(current)
            synch_pos_with_cur(ref_h, ref_v, cur_h, cur_v)
        end
     -- if synctex then
     --     synctextsilh(this_box)
     -- end
        pos_h = ref_h
        pos_v = ref_v
        pos_r = ref_r
    end

end

function drivers.convert(name,box,smode,objnum,specification)

    if box then
        box = tonut(box)
    else
        report("error on converter, no box")
        return
    end

    local driver = instances[name]
    if driver then
        -- tracing
    else
        return
    end

    local actions     = driver.actions
    local flushers    = driver.flushers

    initialize        = actions.initialize
    finalize          = actions.finalize
    updatefontstate   = actions.updatefontstate

    pushorientation   = flushers.pushorientation
    poporientation    = flushers.poporientation

    flushcharacter    = flushers.character
    flushrule         = flushers.rule
    flushsimplerule   = flushers.simplerule
    flushlatelua      = flushers.latelua
    flushspecial      = flushers.special
    flushpdfliteral   = flushers.pdfliteral
    flushpdfsetmatrix = flushers.pdfsetmatrix
    flushpdfsave      = flushers.pdfsave
    flushpdfrestore   = flushers.pdfrestore
    flushpdfimage     = flushers.pdfimage

    reset_dir_stack()
    reset_state()

    shippingmode = smode

 -- synctex      = texget("synctex")

 -- if synctex then
 --     synctexsheet(1000)
 -- end

    local width, height, depth = getwhd(box)

    local total = height + depth

    ----- v_offset_par    = 0
    ----- h_offset_par    = 0

    local max_v = total -- + v_offset_par
    local max_h = width -- + h_offset_par

    if height > maxdimen or depth > maxdimen or width > maxdimen then
        goto DONE
    end

    if max_v > maxdimen then
        goto DONE
    elseif max_v > abs_max_v then
        abs_max_v = max_v
    end

    if max_h > maxdimen then
        goto DONE
    elseif max_h > abs_max_h then
        abs_max_h = max_h
    end

    if shippingmode == "page" then

        -- We have zero offsets in ConTeXt.

        local pagewidth, pageheight = getpagedimensions()
     -- local h_offset_par, v_offset_par = texget("hoffset"), texget("voffset")

     -- page_h_origin = trueinch
     -- page_v_origin = trueinch

        pos_r = lefttoright_code

        if pagewidth > 0 then
            page_size_h = pagewidth
        else
            page_size_h = width
        end

        if page_size_h == 0 then
            page_size_h = width
        end

        if pageheight > 0 then
            page_size_v = pageheight
        else
            page_size_v = total
        end

        if page_size_v == 0 then
            page_size_v = total
        end

        local refpoint_h = 0           -- + page_h_origin + h_offset_par
        local refpoint_v = page_size_v -- - page_v_origin - v_offset_par

        synch_pos_with_cur(refpoint_h,refpoint_v,0,height)

    else

     -- page_h_origin = 0
     -- page_v_origin = 0
        page_size_h   = width
        page_size_v   = total
        pos_r         = getdirection(box)
        pos_v         = depth
        pos_h         = pos_r == righttoleft_code and width or 0

    end

    shipbox_ref_h = pos_h
    shipbox_ref_v = pos_v

    initialize {
        shippingmode = smode, -- target
        boundingbox  = { 0, 0, page_size_h, page_size_v },
        objectnumber = objnum,
    }

    if getid(box) == vlist_code then
        vlist_out(box)
    else
        hlist_out(box)
    end

    ::DONE::

 -- if synctex then
 --     synctexteehs()
 -- end

    finalize(objnum,specification)
    shippingmode = "none"
end

-- synctex
-- flush node list
-- reset dead cycles

-- This is now a bit messy but will be done better. --

local initialized = false
local shipouts    = { }

local function wrapup()
    statistics.starttiming(shipouts)
    drivers.wrapup("pdf")
    statistics.stoptiming(shipouts)
end

local function report()
    return statistics.elapsedseconds(shipouts)
end

local function shipout(boxnumber)
    callbacks.functions.start_page_number()
    statistics.starttiming(shipouts)
    drivers.convert("pdf",tex.box[boxnumber],"page")
    statistics.stoptiming(shipouts)
    callbacks.functions.stop_page_number()
end

-- For the moment this is sort of messy and pdf ... will change

local function enableshipout()
    if not initialized then
        -- install new functions in pdf namespace
        updaters.apply("backend.update.pdf")
        -- install new functions in lpdf namespace
        updaters.apply("backend.update.lpdf")
        -- adapt existing shortcuts to lpdf namespace
        updaters.apply("backend.update.tex")
        -- adapt existing shortcuts to tex namespace
        updaters.apply("backend.update")
        --
        directives.enable("graphics.uselua")
        --
        if rawget(pdf,"setforcefile") then
            pdf.setforcefile(false) -- default anyway
        end
        --
        local pdfname = tex.jobname .. ".pdf"
        local tmpname = "l_m_t_x_" .. pdfname
        os.remove(tmpname)
        if lfs.isfile(tmpname) then
            report("file %a can't be opened, aborting",tmpname)
            os.exit()
        end
        lpdf.openfile(tmpname)
        --
        luatex.registerstopactions(1,function()
            lpdf.finalizedocument()
            lpdf.closefile()
        end)
        --
        luatex.registerpageactions(1,function()
            lpdf.finalizepage(true)
        end)
        --
        luatex.wrapup(function()
            local ok = true
            if lfs.isfile(pdfname) then
                os.remove(pdfname)
            end
            if lfs.isfile(pdfname) then
                ok = false
            else
                os.rename(tmpname,pdfname)
                if lfs.isfile(tmpname) then
                    ok = false
                end
            end
            if not ok then
                print(formatters["\nerror in renaming %a to %a\n"](tmpname,pdfname))
            end
        end)
        --
        fonts.constructors.autocleanup = false
        --
        lpdf.registerdocumentfinalizer(wrapup,nil,"wrapping up")
        --
        statistics.register("pdf generation time (minus font inclusion)",report)
        --
        environment.lmtxmode = true
        --
        initialized = true
    end
end

interfaces.implement {
    name      = "shipoutpage",
    arguments = "integer",
    actions   = shipout,
}

interfaces.implement {
    name    = "enableshipout",
    actions = enableshipout,
}
