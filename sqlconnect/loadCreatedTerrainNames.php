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

        $steamId = $conn->real_escape_string($_POST["SteamId"]);
    
        $conn->begin_transaction();
        try{
            $retreiveTerrainNames = $conn->prepare("SELECT TerrainName, TerrainId FROM Terrains WHERE UserId = (SELECT Id FROM Users WHERE SteamId = ?)");
            $retreiveTerrainNames->bind_param("s", $steamId);
            $retreiveTerrainNames->execute();
            $terrainNames = $retreiveTerrainNames->get_result();

            if (!$terrainNames){
                throw new Exception("Query failed: " . $conn->error);
            }
            
            while ($row = $terrainNames->fetch_assoc()) {
                $response["data"][] = [
                    "TerrainName" => (string)$row["TerrainName"],
                    "TerrainId" => (int)$row["TerrainId"]

                ];
            }

           
            $conn->commit();
            $response['success'] = true;
            $response['message'] = "Successfully added user";
            
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

