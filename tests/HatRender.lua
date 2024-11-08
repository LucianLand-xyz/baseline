-- Hat v1.0.1a

assetUrl, fileExtension, x, y, baseUrl = "http://moonic.wtf/asset/?id=1", "png", "1680", "1680", "http://moonic.wtf/"

pcall(function() game:GetService("ContentProvider"):SetBaseUrl(baseUrl) end)
game:GetService("ScriptContext").ScriptsDisabled = true

game:GetObjects(assetUrl)[1].Parent = workspace

return game:GetService("ThumbnailGenerator"):Click(fileExtension, x, y, --[[hideSky = ]] true, --[[crop =]] true)