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
                INSERT INTO Terrains (UserId, TerrainName, width, height, isolevel, waterLevel, lerp, terracing, terraceHeight)
                VALUES ((SELECT Id FROM Users WHERE SteamId = ?), ?, ?, ?, ?, ?, ?, ?, ?)
                ON DUPLICATE KEY UPDATE TerrainId = LAST_INSERT_ID(TerrainID),
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
        $insertTerrain->bind_param(
            "isiiiiiii",
            $steamId,
            $terrainName,
            $width,
            $height,
            $isolevel,
            $waterLevel,
            $lerp,
            $terracing,
            $terraceHeight
        );
        $insertTerrain->execute();
        $terrainId = $conn->insert_id;

        $insertNoise = $conn->prepare("
                    INSERT INTO NoiseSettings (
                        TerrainId, activated, noiseGeneratorType, selectedNoiseDimension, noiseDimension,
                        selectedNoiseType, noiseType, selectedNoiseFractalType, noiseFractalType,
                        selectedRotationType3D, rotationType3D, noiseSeed, noiseFractalOctaves,
                        noiseFractalLacunarity, noiseFractalGain, fractalWeightedStrength, noiseFrequency,
                        domainWarpToggle, selectedDomainWarpType, domainWarpType, selectedDomainWarpFractalType,
                        domainWarpFractalType, domainWarpAmplitude, domainWarpSeed, domainWarpFractalOctaves,
                        domainWarpFractalLacunarity, domainWarpFractalGain, domainWarpFrequency,
                        selectedCellularDistanceFunction, cellularDistanceFunction, selectedCellularReturnType,
                        cellularReturnType, cellularJitter, noiseScale, width
                    ) VALUES (
                        ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?
                    )
                        ON DUPLICATE KEY UPDATE
                        activated = VALUES(activated),
                        selectedNoiseDimension = VALUES(selectedNoiseDimension),
                        noiseDimension = VALUES(noiseDimension),
                        selectedNoiseType = VALUES(selectedNoiseType),
                        noiseType = VALUES(noiseType),
                        selectedNoiseFractalType = VALUES(selectedNoiseFractalType),
                        noiseFractalType = VALUES(noiseFractalType),
                        selectedRotationType3D = VALUES(selectedRotationType3D),
                        rotationType3D = VALUES(rotationType3D),
                        noiseSeed = VALUES(noiseSeed),
                        noiseFractalOctaves = VALUES(noiseFractalOctaves),
                        noiseFractalLacunarity = VALUES(noiseFractalLacunarity),
                        noiseFractalGain = VALUES(noiseFractalGain),
                        fractalWeightedStrength = VALUES(fractalWeightedStrength),
                        noiseFrequency = VALUES(noiseFrequency),
                        domainWarpToggle = VALUES(domainWarpToggle),
                        selectedDomainWarpType = VALUES(selectedDomainWarpType),
                        domainWarpType = VALUES(domainWarpType),
                        selectedDomainWarpFractalType = VALUES(selectedDomainWarpFractalType),
                        domainWarpFractalType = VALUES(domainWarpFractalType),
                        domainWarpAmplitude = VALUES(domainWarpAmplitude),
                        domainWarpSeed = VALUES(domainWarpSeed),
                        domainWarpFractalOctaves = VALUES(domainWarpFractalOctaves),
                        domainWarpFractalLacunarity = VALUES(domainWarpFractalLacunarity),
                        domainWarpFractalGain = VALUES(domainWarpFractalGain),
                        domainWarpFrequency = VALUES(domainWarpFrequency),
                        selectedCellularDistanceFunction = VALUES(selectedCellularDistanceFunction),
                        cellularDistanceFunction = VALUES(cellularDistanceFunction),
                        selectedCellularReturnType = VALUES(selectedCellularReturnType),
                        cellularReturnType = VALUES(cellularReturnType),
                        cellularJitter = VALUES(cellularJitter),
                        noiseScale = VALUES(noiseScale),
                        width = VALUES(width)
                ");

        foreach ($terrainSettings['noiseSettings'] as $noise) {
            $activated = $noise['activated'] ? 1 : 0;
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
            $terrainWidth = $terrainSettings['width'];
            $insertNoise->bind_param(
                "iiiiiiiiiiiiiddddiiiiidiidddiiiiddi",
                $terrainId,
                $activated,
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
                $noiseScale,
                $terrainWidth
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