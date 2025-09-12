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
    'data' => []
];

try {
    $conn = new mysqli($host, $username, $password, $dbname);
    // Check connection
    if ($conn->connect_errno) {
        throw new Exception("Connection failed: " . $conn->connect_error);
    }

    $terrainId = $conn->real_escape_string($_POST["terrainId"]);

    $conn->begin_transaction();
    try {
        $terrainStmt = $conn->prepare("
            SELECT width, height, isolevel, waterLevel, lerp, terracing, terraceHeight
            FROM Terrains
            WHERE TerrainId = ?
        ");
        $terrainStmt->bind_param("i", $terrainId);
        $terrainStmt->execute();
        $terrainResult = $terrainStmt->get_result();

        if (!$terrainResult || $terrainResult->num_rows === 0) {
            throw new Exception("No terrain found for TerrainId: $terrainId");
        }

        $terrainRow = $terrainResult->fetch_assoc();

        $noiseStmt = $conn->prepare("
            SELECT * 
            FROM NoiseSettings
            WHERE TerrainId = ?
        ");
        $noiseStmt->bind_param("i", $terrainId);
        $noiseStmt->execute();
        $noiseResult = $noiseStmt->get_result();

        if (!$noiseResult) {
            throw new Exception("Query failed: " . $conn->error);
        }

        $noiseSettings = [];
        while ($row = $noiseResult->fetch_assoc()) {
            $noiseSettings[] = [
                'activated' => (bool) $row['activated'],
                'noiseGeneratorType' => (int) $row['noiseGeneratorType'],
                'selectedNoiseDimension' => (int) $row['selectedNoiseDimension'],
                'noiseDimension' => (int) $row['noiseDimension'],
                'selectedNoiseType' => (int) $row['selectedNoiseType'],
                'noiseType' => (int) $row['noiseType'],
                'selectedNoiseFractalType' => (int) $row['selectedNoiseFractalType'],
                'noiseFractalType' => (int) $row['noiseFractalType'],
                'selectedRotationType3D' => (int) $row['selectedRotationType3D'],
                'rotationType3D' => (int) $row['rotationType3D'],
                'noiseSeed' => (int) $row['noiseSeed'],
                'noiseFractalOctaves' => (int) $row['noiseFractalOctaves'],
                'noiseFractalLacunarity' => (float) $row['noiseFractalLacunarity'],
                'noiseFractalGain' => (float) $row['noiseFractalGain'],
                'fractalWeightedStrength' => (float) $row['fractalWeightedStrength'],
                'noiseFrequency' => (float) $row['noiseFrequency'],
                'domainWarpToggle' => (bool) $row['domainWarpToggle'],
                'selectedDomainWarpType' => (int) $row['selectedDomainWarpType'],
                'domainWarpType' => (int) $row['domainWarpType'],
                'selectedDomainWarpFractalType' => (int) $row['selectedDomainWarpFractalType'],
                'domainWarpFractalType' => (int) $row['domainWarpFractalType'],
                'domainWarpAmplitude' => (float) $row['domainWarpAmplitude'],
                'domainWarpSeed' => (int) $row['domainWarpSeed'],
                'domainWarpFractalOctaves' => (int) $row['domainWarpFractalOctaves'],
                'domainWarpFractalLacunarity' => (float) $row['domainWarpFractalLacunarity'],
                'domainWarpFractalGain' => (float) $row['domainWarpFractalGain'],
                'domainWarpFrequency' => (float) $row['domainWarpFrequency'],
                'selectedCellularDistanceFunction' => (int) $row['selectedCellularDistanceFunction'],
                'cellularDistanceFunction' => (int) $row['cellularDistanceFunction'],
                'selectedCellularReturnType' => (int) $row['selectedCellularReturnType'],
                'cellularReturnType' => (int) $row['cellularReturnType'],
                'cellularJitter' => (float) $row['cellularJitter'],
                'noiseScale' => (float) $row['noiseScale'],
                'width' => (int) $row['width']
            ];
        }

        // 3. Build the final TerrainSettings response
        $response['data'] = [
            'width' => (int) $terrainRow['width'],
            'height' => (int) $terrainRow['height'],
            'isolevel' => (float) $terrainRow['isolevel'],
            'waterLevel' => (int) $terrainRow['waterLevel'],
            'lerp' => (bool) $terrainRow['lerp'],
            'terracing' => (bool) $terrainRow['terracing'],
            'terraceHeight' => (int) $terrainRow['terraceHeight'],
            'noiseSettings' => $noiseSettings
        ];

        $conn->commit();
        $response['success'] = true;
        $response['message'] = "Successfully Retreived Terrain Data";

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