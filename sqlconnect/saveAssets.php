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
    ];

    try{
        $conn = new mysqli($host, $username, $password, $dbname);
        // Check connection
        if ($conn->connect_errno) {
            throw new Exception("Connection failed: " . $conn->connect_error);
        }

        //Terrain ID
        $TerrainId = $conn->real_escape_string($_POST["TerrainId"]);

        $conn->begin_transaction();
        try{

            //Clear the asset table, as this ensures only current assets are stored
            $DeleteAssets = $conn->prepare("DELETE FROM ManualAssets WHERE TerrainId = ?");
            $DeleteAssets->bind_param("i", $TerrainId);
            $DeleteAssets->execute();
            
            if (isset($_POST["Assets"])) {

                $assetsJson = json_decode($_POST["Assets"], true);
                $assets = isset($assetsJson["data"]) ? $assetsJson["data"] : $assetsJson;
                if(is_array($assets)){
                    $insertAsset = $conn->prepare("INSERT INTO ManualAssets (TerrainId, AssetId, xPos, yPos, zPos) VALUES (?,?,?,?,?)");

                    foreach($assets as $manAsset){
                        $assetId = $manAsset["AssetId"];
                        $xPos = $manAsset["xPos"];
                        $yPos = $manAsset["yPos"];
                        $zPos = $manAsset["zPos"];

                        $insertAsset->bind_param("iiddd", $TerrainId, $assetId, $xPos, $yPos, $zPos);
                        $insertAsset->execute();
                    }
                }
            }

            
            $conn->commit();
            $response['success'] = true;
            $response['message'] = "Sucessfully Saved Terrain";
            
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

