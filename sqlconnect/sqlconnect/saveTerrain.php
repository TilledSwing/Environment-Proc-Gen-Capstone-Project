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

    try{
        $conn = new mysqli($host, $username, $password, $dbname);
        // Check connection
        if ($conn->connect_errno) {
            throw new Exception("Connection failed: " . $conn->connect_error);
        }

        $steamId = $conn->real_escape_string($_POST["SteamId"]);
        $terrainName = $conn->real_escape_string($_POST["TerrainName"]);

        //Noise Settings
        $NoiseDimensions = $conn->real_escape_string($_POST["NoiseDimensions"]);
        $NoiseTypes = $conn->real_escape_string($_POST["NoiseTypes"]);
        $Seed = $conn->real_escape_string($_POST["Seed"]);
        $Width = $conn->real_escape_string($_POST["Width"]);
        $Height = $conn->real_escape_string($_POST["Height"]);
        $NoiseScale = $conn->real_escape_string($_POST["NoiseScale"]);
        $IsoLevel = $conn->real_escape_string($_POST["IsoLevel"]);
        $Lerp = $conn->real_escape_string($_POST["Lerp"] === "True") ? 1 : 0;
        $NoiseFrequency = $conn->real_escape_string($_POST["NoiseFrequency"]);

        //Domain Warp Settings
        $WarpType = $conn->real_escape_string($_POST["WarpType"]);
        $WarpFractalTypes = $conn->real_escape_string($_POST["WarpFractalTypes"]);
        $WarpAmplitude = $conn->real_escape_string($_POST["WarpAmplitude"]);
        $WarpSeed = $conn->real_escape_string($_POST["WarpSeed"]);
        $WarpFrequency = $conn->real_escape_string($_POST["WarpFrequency"]);
        $WarpFractalOctaves = $conn->real_escape_string($_POST["WarpFractalOctaves"]);
        $WarpFractalLacunarity = $conn->real_escape_string($_POST["WarpFractalLacunarity"]);
        $WarpFractalGain = $conn->real_escape_string($_POST["WarpFractalGain"]);
        $DomainWarp = $conn->real_escape_string($_POST["DomainWarp"] === "True") ? 1 : 0;
        
        //Fractal settings
        $FractalTypes = $conn->real_escape_string($_POST["FractalTypes"]);
        $FractalOctaves = $conn->real_escape_string($_POST["FractalOctaves"]);
        $FractalLacunarity = $conn->real_escape_string($_POST["FractalLacunarity"]);
        $FractalGain = $conn->real_escape_string($_POST["FractalGain"]);
        $FractalWeightedStrength = $conn->real_escape_string($_POST["FractalWeightedStrength"]);

        $conn->begin_transaction();
        try{

            $insertTerrain = $conn->prepare("INSERT INTO Terrains (UserId, TerrainName) VALUES ((SELECT Id FROM Users WHERE SteamId = ?), ?) 
                                            ON DUPLICATE KEY UPDATE TerrainId = LAST_INSERT_ID(TerrainId)");
            $insertTerrain->bind_param("is", $steamId, $terrainName);
            $insertTerrain->execute();
            $terrainId = $conn->insert_id;


            $updateNoiseSettings = $conn->prepare("INSERT INTO NoiseSettings (TerrainId, NoiseDimensions, NoiseTypes, Seed, Width, Height, NoiseScale, IsoLevel, Lerp, NoiseFrequency)
                                                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?) ON DUPLICATE KEY UPDATE
                                                    NoiseDimensions = VALUES(NoiseDimensions), NoiseTypes = VALUES(NoiseTypes), Seed = VALUES(Seed), Width = VALUES(Width), Height = VALUES(Height),
                                                    NoiseScale = VALUES(NoiseScale), IsoLevel = VALUES(IsoLevel), Lerp = VALUES(Lerp), NoiseFrequency = VALUES(NoiseFrequency)");
            $updateNoiseSettings->bind_param("issiiiddid", $terrainId, $NoiseDimensions, $NoiseTypes, $Seed, $Width, $Height, $NoiseScale, $IsoLevel, $Lerp, $NoiseFrequency);
            $updateNoiseSettings->execute();
            
            $updateDomainWarpSettings = $conn->prepare("INSERT INTO DomainWarpSettings (TerrainId, WarpType, WarpFractalTypes, WarpAmplitude, WarpSeed, WarpFrequency, WarpFractalOctaves, WarpFractalLacunarity, 
                                                    WarpFractalGain, DomainWarp)
                                                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?) ON DUPLICATE KEY UPDATE
                                                    WarpType = VALUES(WarpType), WarpFractalTypes = VALUES(WarpFractalTypes), WarpAmplitude = VALUES(WarpAmplitude), WarpSeed = VALUES(WarpSeed), WarpFrequency = VALUES(WarpFrequency),
                                                    WarpFractalOctaves = VALUES(WarpFractalOctaves), WarpFractalLacunarity = VALUES(WarpFractalLacunarity), WarpFractalGain = VALUES(WarpFractalGain), DomainWarp = VALUES(DomainWarp)");
            $updateDomainWarpSettings->bind_param("issdididdi", $terrainId, $WarpType, $WarpFractalTypes, $WarpAmplitude, $WarpSeed, $WarpFrequency, $WarpFractalOctaves, $WarpFractalLacunarity, 
                                                                $WarpFractalGain, $DomainWarp);
            $updateDomainWarpSettings->execute();

            $updateFractalSettings = $conn->prepare("INSERT INTO FractalSettings (TerrainId, FractalTypes, FractalOctaves, FractalLacunarity, FractalGain, FractalWeightedStrength)
                                    VALUES (?, ?, ?, ?, ?, ?) ON DUPLICATE KEY UPDATE
                                    FractalTypes = VALUES(FractalTypes), FractalOctaves = VALUES(FractalOctaves), FractalLacunarity = VALUES(FractalLacunarity), 
                                    FractalGain = VALUES(FractalGain), FractalWeightedStrength = VALUES(FractalWeightedStrength)");
            $updateFractalSettings->bind_param("isiddd", $terrainId, $FractalTypes, $FractalOctaves, $FractalLacunarity, $FractalGain, $FractalWeightedStrength);
            $updateFractalSettings->execute();

            
            $conn->commit();
            $response['success'] = true;
            $response['message'] = "Sucessfully Saved Terrain";
            $response['terrainId'] = $terrainId;
            
        } catch(Exception $e){
            $conn->rollback();
            throw $e;
        }

    } catch (Exception $e){
        $response['message'] = "Error: " . $e->getMessage();
    } finally {
        if (isset($conn)){
            $conn->close();
        }
        echo json_encode($response);
    }
   

?>

