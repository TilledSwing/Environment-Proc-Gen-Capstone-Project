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
            $retreiveTerrainAssets = $conn->prepare("SELECT * FROM ManualAssets WHERE TerrainId = ?");
            $retreiveTerrainAssets->bind_param("i", $terrainId);
            $retreiveTerrainAssets->execute();
            $terrainAssets = $retreiveTerrainAssets->get_result();

            if (!$terrainAssets){
                throw new Exception("Query failed: " . $conn->error);
            }
            
            while ($row = $terrainAssets->fetch_assoc()) {
                $response["data"][] = [
                    "AssetId" => (string)$row["AssetId"],
                    "xPos" => (float)$row["xPos"],
                    "yPos" => (float)$row["yPos"],
                    "zPos" => (float)$row["zPos"],
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

