local drawableSprite = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")

local entity = {}

entity.name = "ChroniaHelper/PasswordKeyboard"

--entity.justification = { 0.5, 1.0 }
entity.placements = {
    name = "normal",
    data = {
        --width = 16,
        --height = 16,

        mode = 0,
        flagToEnable = "",
        password = "",
        --rightDialog = "rightDialog",
        --wrongDialog = "wrongDialog",
        caseSensitive = false,
        useTimes = -1,
        accessZone = "-16,0,32,8",
        accessZoneIndicator = false,
        --globalFlag = false,
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
        elementOptions = {
            fieldType = "integer",
        },
    }
}

entity.sprite = function(room, entity)
    local defaultTexture = "PasswordKeyboard/keyboard"
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
