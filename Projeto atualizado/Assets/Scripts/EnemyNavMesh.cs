using UnityEngine;
using UnityEngine.AI;
public class EnemyNavMesh : MonoBehaviour
{
    public Transform player;
    public Transform Cover;
    public NavMeshAgent agent;
    public Transform[] waypoints;
    public LayerMask layerMask;
    public float losePlayerDistance = 15f;

    // Update is called once per frame

    public enum EnemyState
    {
        WayPatrol,RandomPatrol, Pursuit, Escape
    }
    public EnemyState currentState = EnemyState.WayPatrol;

    void Update()
    {
        FiniteStateMachine();
    }
    public void ChangeState(EnemyState newState)
    {
        currentState = newState;
    }
    void FiniteStateMachine()
    {
        switch (currentState)
        {
            case EnemyState.Pursuit:
                Pursuit();
                break;
            case EnemyState.WayPatrol:
                WayPatrol();
                break;
            case EnemyState.Escape:
                Run();
                break;
            default:
                break;
        }
    }
    void Pursuit()
    {
        if(player != null)
        {
            agent.stoppingDistance = 6f;
            agent.SetDestination(player.position);
            transform.LookAt(player);
        }
        float distanceToPlayer = Vector3.Distance(transform.position,player.position);
        if(distanceToPlayer > losePlayerDistance)
        {
            ChangeState(EnemyState.WayPatrol);
        }
    }
    void WayPatrol()
    {
        agent.stoppingDistance = 0f;
        if(waypoints.Length !=0 && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            int randomIndex = Random.Range(0, waypoints.Length);
            agent.SetDestination(waypoints[randomIndex].position);
        }
        SearchPlayer();
    }
    void SearchPlayer()
    {
        if(Physics.Raycast(transform.position,transform.forward, 
        out RaycastHit hit, 20f, layerMask)){
            ChangeState(EnemyState.Pursuit);
                CallBackup();
        }
    }
    void CallBackup()
    {
        EnemyNavMesh[] todosInimigos = FindObjectsByType<EnemyNavMesh>();
        foreach (EnemyNavMesh aliado in todosInimigos)
        {
            float distancia = Vector3.Distance(transform.position,aliado.transform.position);
            if (distancia < 10f)
            {
                aliado.ChangeState(EnemyState.Pursuit);
            }
            
        }
    }
    void Run()
    {
        if(player != null)
        {
            agent.stoppingDistance = 0.5f;
            agent.SetDestination(Cover.position);
            transform.LookAt(Cover);
        }
        float dist = Vector3.Distance(transform.position,Cover.position);
        if(dist<0.5f)
        {
            ChangeState(EnemyState.Pursuit);
        }
        
    }
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bala"))
        {
            ChangeState(EnemyState.Escape);
        }
    }
}
