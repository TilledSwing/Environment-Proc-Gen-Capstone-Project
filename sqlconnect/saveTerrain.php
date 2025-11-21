<?php
header('Content-Type: application/json');
$host = "maria.eng.utah.edu";
$dbname = "pegg";
$username = "u1431093";
$password = "wV8MzCJHN04lsDoVMSM7feGGq";

//Struct for return values
$response = [
    'success' => false,
    'message' => 'Initial state',
    'terrainId' => -1,

];

try {
    $conn = new mysqli($host, $username, $password, $dbname);
    // Check connection
    if ($conn->connect_errno) {
        throw new Exception("Connection failed: " . $conn->connect_error);
    }

    $steamId = $conn->real_escape_string($_POST["SteamId"]);
    $terrainName = $conn->real_escape_string($_POST["TerrainName"]);

    $terrainJson = $_POST["TerrainSettings"];
    $terrainSettings = json_decode($terrainJson, true);

    $conn->begin_transaction();
    try {

        $insertTerrain = $conn->prepare("
                        INSERT INTO Terrains (UserId, TerrainName)
                        VALUES ((SELECT Id FROM Users WHERE SteamId = ?), ?) ON DUPLICATE KEY UPDATE TerrainId = LAST_INSERT_ID(TerrainID)");  
        
        $insertTerrain->bind_param(
            "is",
            $steamId,
            $terrainName
        );
        $insertTerrain->execute();
        $terrainId = $conn->insert_id;

        //Delete the old settings if they exist
        $deleteStmt = $conn->prepare("DELETE FROM TerrainSettings WHERE TerrainId = ?");
        $deleteStmt->bind_param("i", $terrainId);
        $deleteStmt->execute();

        $deleteStmt = $conn->prepare("DELETE FROM TerrainNoiseSettings WHERE TerrainId = ?");
        $deleteStmt->bind_param("i", $terrainId);
        $deleteStmt->execute();

        $insertTerrainSettings = $conn->prepare("
                INSERT INTO TerrainSettings (TerrainId,  width, height, isolevel, waterLevel, lerp, terracing, terraceHeight)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?) ON DUPLICATE KEY UPDATE
                width = VALUES(width),
                height = VALUES(height),
                isolevel = VALUES(isolevel),
                waterLevel = VALUES(waterLevel),
                lerp = VALUES(lerp),
                terracing = VALUES(terracing),
                terraceHeight = VALUES(terraceHeight)
            ");

        $width = $terrainSettings['width'];
        $height = $terrainSettings['height'];
        $isolevel = $terrainSettings['isolevel'];
        $waterLevel = $terrainSettings['waterLevel'] ?? 0;
        $lerp = $terrainSettings['lerp'] ? 1 : 0;
        $terracing = $terrainSettings['terracing'] ? 1 : 0;
        $terraceHeight = $terrainSettings['terraceHeight'] ?? 0;
        $insertTerrainSettings->bind_param(
            "iiidiiii",
            $terrainId,
            $width,
            $height,
            $isolevel,
            $waterLevel,
            $lerp,
            $terracing,
            $terraceHeight
        );
        $insertTerrainSettings->execute();

        $insertNoise = $conn->prepare("
                    INSERT INTO TerrainNoiseSettings (
                        TerrainId, activated, remoteTexture, noiseGeneratorType, selectedNoiseDimension, noiseDimensions,
                        selectedNoiseType, noiseType, selectedNoiseFractalType, noiseFractalType,
                        selectedRotationType3D, rotationType3D, noiseSeed, noiseFractalOctaves,
                        noiseFractalLacunarity, noiseFractalGain, fractalWeightedStrength, noiseFrequency, noiseScale,
                        domainWarpToggle, selectedDomainWarpType, domainWarpType, selectedDomainWarpFractalType,
                        domainWarpFractalType, domainWarpAmplitude, domainWarpSeed, domainWarpFractalOctaves,
                        domainWarpFractalLacunarity, domainWarpFractalGain, domainWarpFrequency,
                        selectedCellularDistanceFunction, cellularDistanceFunction, selectedCellularReturnType,
                        cellularReturnType, cellularJitter
                    ) VALUES (
                        ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?
                    )
                ");

        foreach ($terrainSettings['noiseSettings'] as $noise) {
            $activated = $noise['activated'] ? 1 : 0;
            $remoteTexture = json_encode($noise['remoteTexture']);;
            $noiseGeneratorType = $noise['noiseGeneratorType'];
            $selectedNoiseDimension = $noise['selectedNoiseDimension'];
            $noiseDimension = $noise['noiseDimension'];
            $selectedNoiseType = $noise['selectedNoiseType'];
            $noiseType = $noise['noiseType'];
            $selectedNoiseFractalType = $noise['selectedNoiseFractalType'];
            $noiseFractalType = $noise['noiseFractalType'];
            $selectedRotationType3D = $noise['selectedRotationType3D'];
            $rotationType3D = $noise['rotationType3D'];
            $noiseSeed = $noise['noiseSeed'];
            $noiseFractalOctaves = $noise['noiseFractalOctaves'];
            $noiseFractalLacunarity = $noise['noiseFractalLacunarity'];
            $noiseFractalGain = $noise['noiseFractalGain'];
            $fractalWeightedStrength = $noise['fractalWeightedStrength'];
            $noiseFrequency = $noise['noiseFrequency'];
            $domainWarpToggle = $noise['domainWarpToggle'] ? 1 : 0;
            $selectedDomainWarpType = $noise['selectedDomainWarpType'];
            $domainWarpType = $noise['domainWarpType'];
            $selectedDomainWarpFractalType = $noise['selectedDomainWarpFractalType'];
            $domainWarpFractalType = $noise['domainWarpFractalType'];
            $domainWarpAmplitude = $noise['domainWarpAmplitude'];
            $domainWarpSeed = $noise['domainWarpSeed'];
            $domainWarpFractalOctaves = $noise['domainWarpFractalOctaves'];
            $domainWarpFractalLacunarity = $noise['domainWarpFractalLacunarity'];
            $domainWarpFractalGain = $noise['domainWarpFractalGain'];
            $domainWarpFrequency = $noise['domainWarpFrequency'];
            $selectedCellularDistanceFunction = $noise['selectedCellularDistanceFunction'];
            $cellularDistanceFunction = $noise['cellularDistanceFunction'];
            $selectedCellularReturnType = $noise['selectedCellularReturnType'];
            $cellularReturnType = $noise['cellularReturnType'];
            $cellularJitter = $noise['cellularJitter'];
            $noiseScale = $noise['noiseScale'];
            $insertNoise->bind_param(
                "iisiiiiiiiiiiidddddiiiiidiidddiiiid",
                $terrainId,
                $activated,
                $remoteTexture,
                $noiseGeneratorType,
                $selectedNoiseDimension,
                $noiseDimension,
                $selectedNoiseType,
                $noiseType,
                $selectedNoiseFractalType,
                $noiseFractalType,
                $selectedRotationType3D,
                $rotationType3D,
                $noiseSeed,
                $noiseFractalOctaves,
                $noiseFractalLacunarity,
                $noiseFractalGain,
                $fractalWeightedStrength,
                $noiseFrequency,
                $noiseScale,
                $domainWarpToggle,
                $selectedDomainWarpType,
                $domainWarpType,
                $selectedDomainWarpFractalType,
                $domainWarpFractalType,
                $domainWarpAmplitude,
                $domainWarpSeed,
                $domainWarpFractalOctaves,
                $domainWarpFractalLacunarity,
                $domainWarpFractalGain,
                $domainWarpFrequency,
                $selectedCellularDistanceFunction,
                $cellularDistanceFunction,
                $selectedCellularReturnType,
                $cellularReturnType,
                $cellularJitter,
            );
            $insertNoise->execute();
        }

        $conn->commit();
        $response['success'] = true;
        $response['message'] = "Sucessfully Saved Terrain";
        $response['terrainId'] = $terrainId;

    } catch (Exception $e) {
        $conn->rollback();
        throw $e;
    }

} catch (Exception $e) {
    $response['message'] = "Error: " . $e->getMessage();
} finally {
    if (isset($conn)) {
        $conn->close();
    }
    echo json_encode($response);
}


?>