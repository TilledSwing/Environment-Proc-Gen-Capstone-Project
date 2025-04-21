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

    try{
        $conn = new mysqli($host, $username, $password, $dbname);
        // Check connection
        if ($conn->connect_errno) {
            throw new Exception("Connection failed: " . $conn->connect_error);
        }

        $terrainId = $conn->real_escape_string($_POST["terrainId"]);
    
        $conn->begin_transaction();
        try{
            $retreiveTerrainNames = $conn->prepare("
            SELECT * 
            FROM NoiseSettings 
            JOIN DomainWarpSettings ON NoiseSettings.TerrainId = DomainWarpSettings.TerrainId 
            JOIN FractalSettings ON NoiseSettings.TerrainId = FractalSettings.TerrainId 
            WHERE NoiseSettings.TerrainId = ?
        ");
            $retreiveTerrainNames->bind_param("i", $terrainId);
            $retreiveTerrainNames->execute();
            $terrainNames = $retreiveTerrainNames->get_result();

            if (!$terrainNames){
                throw new Exception("Query failed: " . $conn->error);
            }
            
            while ($row = $terrainNames->fetch_assoc()) {
                $response["data"][] = [
                    //Noise Settings
                    "NoiseDimensions" => $row["NoiseDimensions"],
                    "NoiseTypes" => $row['NoiseTypes'],
                    "Seed" => (int)$row["Seed"],
                    "Width" => (int)$row["Width"],
                    "Height" => (int)$row["Height"],
                    "NoiseScale" => (float)$row["NoiseScale"],
                    "IsoLevel" => (float)$row["IsoLevel"],
                    "Lerp" => (bool)$row["Lerp"],
                    "NoiseFrequency" => (float)$row["NoiseFrequency"],

                    // DomainWarpSettings fields
                    "WarpType" => $row["WarpType"], 
                    "WarpFractalTypes" => $row["WarpFractalTypes"],
                    "WarpAmplitude" => (float)$row["WarpAmplitude"],
                    "WarpSeed" => (int)$row["WarpSeed"],
                    "WarpFrequency" => (float)$row["WarpFrequency"],
                    "WarpFractalOctaves" => (int)$row["WarpFractalOctaves"],
                    "WarpFractalLacunarity" => (float)$row["WarpFractalLacunarity"],
                    "WarpFractalGain" => (float)$row["WarpFractalGain"],
                    "DomainWarp" => (bool)$row["DomainWarp"],

                    // FractalSettings fields
                    "FractalTypes" => $row["FractalTypes"],
                    "FractalOctaves" => (int)$row["FractalOctaves"],
                    "FractalLacunarity" => (float)$row["FractalLacunarity"],
                    "FractalGain" => (float)$row["FractalGain"],
                    "FractalWeightedStrength" => (float)$row["FractalWeightedStrength"]
                ];
            }

           
            $conn->commit();
            $response['success'] = true;
            $response['message'] = "Successfully Retreived Terrain Data";
            
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

