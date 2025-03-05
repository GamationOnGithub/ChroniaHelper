local drawableSprite = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")

local entity = {}

entity.name = "ChroniaHelper/PasswordKeyboard"
entity.depth = function(room,entity) return entity.depth or 9000 end
--entity.justification = { 0.5, 1.0 }
entity.placements = {
    name = "normal",
    data = {
        --width = 16,
        --height = 16,
        texture = "ChroniaHelper/PasswordKeyboard/keyboard",
        tag = "passwordKeyboard",
        mode = 1,
        flagToEnable = "",
        password = "",
        characterLimit = 12,
        --rightDialog = "rightDialog",
        --wrongDialog = "wrongDialog",
        caseSensitive = true,
        useTimes = -1,
        accessZone = "-16,0,32,8",
        accessZoneIndicator = false,
        talkIconPosition = "0,-8",
        depth = 9000,
        globalFlag = false,
        toggleFlag = false,
        passwordEncrypted = false,
        showEncryptedPasswordInConsole = false,
    }
}

entity.fieldInformation = {
    mode = {
        fieldType = "integer",
        options = {
            ["Exclusive"] = 0,
            ["Normal"] = 1,
            ["OutputFlag"] = 2
        },
        editable = false
    },
    useTimes = {
        fieldType = "integer"
    },
    accessZone = {
        fieldType = "list",
        minimumElements = 4,
        maximumElements = 4,
        elementOptions = {
            fieldType = "integer",
        },
    },
    tag = {
        allowEmpty = false,
    },
    texture = {
        allowEmpty = false,
    },
    talkIconPosition = {
        fieldType = "list",
        minimumElements = 2,
        maximumElements = 2
    },
    depth = require("mods").requireFromPlugin("helpers.field_options").depths,
    characterLimit = {
        minimumValue = 1,
        fieldType = "integer",
    }
}

entity.sprite = function(room, entity)
    --local defaultTexture = "ChroniaHelper/PasswordKeyboard/keyboard"
    local defaultTexture = entity.texture
    local sprites = {}

    sprite = drawableSprite.fromTexture(defaultTexture, entity)
    table.insert(sprites, sprite)

    local str = entity.accessZone -- ���ָ���ַ���
    local parameters = {} -- ���ڴ洢�ָ��Ĳ���
    local start = 1 -- �Ӵ�����ʼλ��

    -- ѭ�����Ҷ��ţ��ָ��ַ���
    while true do
        local commaPos = string.find(str, ",", start) -- ������һ�����ŵ�λ��
        if commaPos == nil then -- ���û���ҵ����ţ�˵�������һ������
            table.insert(parameters, string.sub(str, start)) -- ������һ������
            break -- �˳�ѭ��
        else
            table.insert(parameters, string.sub(str, start, commaPos - 1)) -- ����ҵ��Ĳ���
            start = commaPos + 1 -- ������ʼλ�õ���һ�������Ŀ�ʼ
        end
    end

    -- �趨һ�����α�־����
    local rectangle = drawableRectangle.fromRectangle("bordered",entity.x + tonumber(parameters[1]),entity.y + tonumber(parameters[2]),tonumber(parameters[3]),tonumber(parameters[4]),{0,0,0,0.01},{255,255,255,1})

    if entity.accessZoneIndicator then
        table.insert(sprites, rectangle)
    end
    
    return sprites
end

return entity
