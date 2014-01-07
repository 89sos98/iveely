start "StateCenter" Iveely.CloudComputing.StateCenter.exe
start "Supervisor" Iveely.CloudComputing.Supervisor.exe
start "Worker:7081" Iveely.CloudComputing.Worker.exe 7081
start "Worker:7082" Iveely.CloudComputing.Worker.exe 7082
start "Merger" Iveely.CloudComputing.Merger.exe
start "Cacher" Iveely.CloudComputing.Cacher.exe
